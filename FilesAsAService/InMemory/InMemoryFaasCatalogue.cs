using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.InMemory
{
    /// <summary>
    /// Catalogue that is stored in memory.
    /// </summary>
    public class InMemoryFaasCatalogue : IFaasCatalogue
    {
        /// <summary>
        /// Protect access to the internal storage.
        /// </summary>
        private readonly Semaphore _lock = new Semaphore(1, 1);

        /// <summary>
        /// The size of the pages for listing.
        /// </summary>
        private const int PageSize = 100;

        /// <summary>
        /// When data is deleted, the record is set to null.
        /// This sames recalculating the whole index.
        /// TODO think about cleaning this up vs back filling the gaps.
        /// </summary>
        private readonly List<FaasFileHeader?> _data = new List<FaasFileHeader?>();
        private readonly Dictionary<Guid, int> _fileIdIndex = new Dictionary<Guid, int>();
        
        /// <inheritdoc cref="GetAsync"/>
        public ValueTask<FaasFileHeader?> GetAsync(Guid fileId, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                return GetNoLock(fileId, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Gets a header without using a lock.
        /// </summary>
        /// <param name="fileId">The file id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The header, or null.</returns>
        private ValueTask<FaasFileHeader?> GetNoLock(Guid fileId, CancellationToken cancellationToken)
        {
            // Return null of the id isn't found.
            if (!_fileIdIndex.ContainsKey(fileId))
                return new ValueTask<FaasFileHeader?>((FaasFileHeader?) null);

            // Return the header.
            var header = _data[_fileIdIndex[fileId]];
            return new ValueTask<FaasFileHeader?>(new FaasFileHeader(header));
        }
        
        /// <inheritdoc cref="ListAsync"/>
        public ValueTask<IEnumerable<FaasFileHeader>> ListAsync(int pageNumber, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                return new ValueTask<IEnumerable<FaasFileHeader>>(_data.Skip(pageNumber * PageSize).Take(PageSize).Select(row => new FaasFileHeader(row)));
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc cref="StartCreateAsync"/>
        public ValueTask<FaasFileVersionId> StartCreateAsync(string name, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                // Create the header.
                var fileHeader = new FaasFileHeader
                {
                    FileId = Guid.NewGuid(),
                    Name = name,
                    DateCreatedUtc = DateTime.UtcNow
                };
                fileHeader.Versions = new[]
                {
                    new FaasFileHeaderVersion
                    {
                        VersionId = Guid.NewGuid(),
                        DateCreatedUtc = fileHeader.DateCreatedUtc,
                        Status = FaasFileHeaderVersionStatus.Writing
                    }
                };

                // Add it and index it.
                _data.Add(fileHeader);
                _fileIdIndex.Add(fileHeader.FileId, _data.Count - 1);
                
                // Return it.
                return new ValueTask<FaasFileVersionId>(new FaasFileVersionId(fileHeader.FileId, fileHeader.Versions[0].VersionId));
            }
            finally
            {
                _lock.Release();
            }
        }
        
        /// <inheritdoc cref="StartWritingAsync"/>
        public async ValueTask<FaasFileVersionId> StartWritingAsync(Guid fileId, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                // Get the header. If its not found, throw.
                var header = await GetNoLock(fileId, cancellationToken);
                if (header == null)
                    throw new FaasFileNotFoundException();

                // Create the new version.
                var newVersion = new FaasFileHeaderVersion
                {
                    VersionId = Guid.NewGuid(),
                    DateCreatedUtc = DateTime.UtcNow,
                    Status = FaasFileHeaderVersionStatus.Writing
                };

                // Add it to the header.
                header.Versions = header.Versions.Concat(new[] {newVersion}).ToArray();

                // Return it.
                return new FaasFileVersionId(fileId, newVersion.VersionId);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc cref="CompleteWritingAsync"/>
        public async ValueTask CompleteWritingAsync(Guid fileId, Guid versionId, long length, byte[] hash, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                // Get the header. If its not found, throw.
                var header = await GetNoLock(fileId, cancellationToken);
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
                
                // Update stored copy.
                _data[_fileIdIndex[fileId]] = header;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc cref="CancelWritingAsync"/>
        public async Task CancelWritingAsync(Guid fileId, Guid versionId, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                // Get the header. If its not found, throw.
                var header = await GetNoLock(fileId, cancellationToken);
                if (header == null)
                    throw new FaasFileNotFoundException();

                // Check that the header can be completed.
                var version = header.Versions.FirstOrDefault(v => v.VersionId == versionId);
                if (version == null)
                    throw new FaasFileVersionNotFoundException();
                if (version.Status != FaasFileHeaderVersionStatus.Writing)
                    throw new FaasInvalidOperationException();
                
                // If there is only one version, the whole header can go.
                if (header.Versions.Length == 1)
                {
                    // Clear the header. 
                    // Not deleting it since it will mess with indexes.
                    _data[_fileIdIndex[fileId]] = null;

                    // Delete the index.
                    _fileIdIndex.Remove(fileId);
                }
                else
                {
                    header.Versions = header.Versions.Where(v => v.VersionId != version.VersionId).ToArray();
                }
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}