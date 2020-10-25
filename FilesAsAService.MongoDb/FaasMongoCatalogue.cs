using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.MongoDb.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FilesAsAService.MongoDb
{
    public class FaasMongoCatalogue : IFaasCatalogue
    {
        /// <summary>
        /// The size of the pages for listing.
        /// </summary>
        private const int PageSize = 100;
        
        private readonly MongoClient _client;

        private readonly IMongoCollection<FaasFileHeaderMongoEntity> _collection;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="name">The name. The message bus will use this to locate it.</param>
        /// <param name="client"></param>
        /// <param name="database"></param>
        public FaasMongoCatalogue(string name, MongoClient client, string database)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _collection = _client
                .GetDatabase(database)
                .GetCollection<FaasFileHeaderMongoEntity>(name);
        }
        
        /// <inheritdoc cref="Name"/>
        public string Name { get; }

        private FilterDefinition<FaasFileHeaderMongoEntity> GetItemFilter(Guid fileId)
        {
            return Builders<FaasFileHeaderMongoEntity>.Filter.Eq("FileId", fileId.ToString());
        }
        
        /// <inheritdoc cref="GetAsync"/>
        public async ValueTask<FaasFileHeader?> GetAsync(Guid fileId, CancellationToken cancellationToken)
        {
            var result = await GetBsonAsync(fileId, cancellationToken);
            return result?.ToFaasFileHeader();
        }
        
        public async ValueTask<FaasFileHeaderMongoEntity?> GetBsonAsync(Guid fileId, CancellationToken cancellationToken)
        {
            var filter = GetItemFilter(fileId);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc cref="ListAsync"/>
        public async ValueTask<IEnumerable<FaasFileHeader>> ListAsync(int pageNumber, CancellationToken cancellationToken)
        {
            var result = await _collection.Find(new BsonDocument()).Skip(pageNumber * PageSize).Limit(PageSize).ToListAsync(cancellationToken);
            return result.Select(row => row.ToFaasFileHeader());
        }

        /// <inheritdoc cref="StartCreateAsync"/>
        public async ValueTask<FaasFileVersionId> StartCreateAsync(string name, CancellationToken cancellationToken)
        {
            // Create the header.
            var fileHeader = new FaasFileHeaderMongoEntity
            {
                FileId = Guid.NewGuid(),
                Name = name,
                DateCreatedUtc = DateTime.UtcNow
            };
            fileHeader.Versions = new[]
            {
                new FaasFileHeaderVersionMongoEntity
                {
                    VersionId = Guid.NewGuid(),
                    DateCreatedUtc = fileHeader.DateCreatedUtc,
                    Status = FaasFileHeaderVersionStatus.Writing
                }
            };

            await _collection.InsertOneAsync(fileHeader, cancellationToken: cancellationToken);
            return new FaasFileVersionId(fileHeader.FileId, fileHeader.Versions[0].VersionId);
        }

        /// <inheritdoc cref="StartWritingAsync"/>
        public async ValueTask<FaasFileVersionId> StartWritingAsync(Guid fileId, CancellationToken cancellationToken)
        {
            // Get the header. If its not found, throw.
            var header = await GetBsonAsync(fileId, cancellationToken);
            if (header == null)
                throw new FaasFileNotFoundException();

            // Create the new version.
            var newVersion = new FaasFileHeaderVersionMongoEntity
            {
                VersionId = Guid.NewGuid(),
                DateCreatedUtc = DateTime.UtcNow,
                Status = FaasFileHeaderVersionStatus.Writing
            };

            // Add it to the header.
            header.Versions = header.Versions.Concat(new[] {newVersion}).ToArray();
            
            var filter = GetItemFilter(fileId);
            await _collection.ReplaceOneAsync(filter, header, new ReplaceOptions(), cancellationToken);

            // Return it.
            return new FaasFileVersionId(fileId, newVersion.VersionId);
        }

        /// <inheritdoc cref="CompleteWritingAsync"/>
        public async ValueTask CompleteWritingAsync(Guid fileId, Guid versionId, long length, byte[] hash, CancellationToken cancellationToken)
        {
            // Get the header. If its not found, throw.
            var header = await GetBsonAsync(fileId, cancellationToken);
            if (header == null)
                throw new FaasFileNotFoundException();

            // Check that the header can be completed.
            var version = header.Versions.FirstOrDefault(v => v.VersionId == versionId);
            if (version == null)
                throw new FaasFileVersionNotFoundException();
            if (version.Status != FaasFileHeaderVersionStatus.Writing)
                throw new FaasInvalidOperationException();

            // Update the fields.
            version.Length = length;
            version.Hash = hash;
            version.Status = FaasFileHeaderVersionStatus.Ok;
            header.VersionId = versionId;
            
            var filter = GetItemFilter(fileId);
            await _collection.ReplaceOneAsync(filter, header, new ReplaceOptions(), cancellationToken);
        }

        /// <inheritdoc cref="CancelWritingAsync"/>
        public async Task CancelWritingAsync(Guid fileId, Guid versionId, CancellationToken cancellationToken)
        {
            // Get the header. If its not found, throw.
            var header = await GetBsonAsync(fileId, cancellationToken);
            if (header == null)
                throw new FaasFileNotFoundException();

            // Check that the header can be completed.
            var version = header.Versions.FirstOrDefault(v => v.VersionId == versionId);
            if (version == null)
                throw new FaasFileVersionNotFoundException();
            if (version.Status != FaasFileHeaderVersionStatus.Writing)
                throw new FaasInvalidOperationException();
            
            var filter = GetItemFilter(fileId);
                
            // If there is only one version, the whole header can go.
            if (header.Versions.Length == 1)
            {
                await _collection.DeleteOneAsync(filter, cancellationToken);
            }
            else
            {
                header.Versions = header.Versions.Where(v => v.VersionId != version.VersionId).ToArray();
                
                await _collection.ReplaceOneAsync(filter, header, new ReplaceOptions(), cancellationToken);
            }
        }

        /// <inheritdoc cref="RemoveVersionAsync"/>
        public async Task RemoveVersionAsync(Guid fileId, Guid versionId, CancellationToken cancellationToken)
        {
            // Get the header. If its not found, throw.
            var header = await GetBsonAsync(fileId, cancellationToken);
            if (header == null)
                throw new FaasFileNotFoundException();

            // Check that the header can be completed.
            var version = header.Versions.FirstOrDefault(v => v.VersionId == versionId);
            if (version == null)
                throw new FaasFileVersionNotFoundException();
            if (version.Status == FaasFileHeaderVersionStatus.Writing)
                throw new FaasInvalidOperationException();
            
            var filter = GetItemFilter(fileId);
                
            // If there is only one version, the whole header can go.
            if (header.Versions.Length == 1)
            {
                await _collection.DeleteOneAsync(filter, cancellationToken);
            }
            else
            {
                header.Versions = header.Versions.Where(v => v.VersionId != version.VersionId).ToArray();
                
                await _collection.ReplaceOneAsync(filter, header, new ReplaceOptions(), cancellationToken);
            }
        }
    }
}