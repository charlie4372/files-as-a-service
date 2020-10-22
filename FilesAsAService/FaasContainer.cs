using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.MessageBus;

namespace FilesAsAService
{
    /// <summary>
    /// The container.
    /// </summary>
    public class FaasContainer
    {
        /// <summary>
        /// The catalogue.
        /// </summary>
        private readonly IFaasCatalogue _catalogue;
        
        /// <summary>
        /// The store.
        /// </summary>
        private readonly IFaasFileStore _fileStore;

        private readonly IFaasMessageBus _messageBus;
        
        public string Name { get; }
        
        public FaasContainer(IFaasCatalogue catalogue, IFaasFileStore fileStore, string name, IFaasMessageBus messageBus)
        {
            _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));

            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Creates a file.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="stream">The contents</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The id.</returns>
        public async Task<Guid> CreateAsync(string name, Stream stream, CancellationToken cancellationToken)
        {
            FaasFileVersionId? fileVersionId = null;
            try
            {
                // Start creating the header.
                fileVersionId = await _catalogue.StartCreateAsync(name, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                // We need to know how many bytes are in the file.
                await using var inputByteCounterStream = new ByteCounterStreamProxy(stream, false);
                
                // We need a hash of the file.
                await using var hashStream = new HashGeneratorStreamProxy(inputByteCounterStream, false);

                // Write the contents.
                await _fileStore.CreateAsync(fileVersionId.Value.VersionId, inputByteCounterStream, cancellationToken);

                // Complete the file and make it available.
                await _catalogue.CompleteWritingAsync(fileVersionId.Value.FileId, fileVersionId.Value.VersionId,  inputByteCounterStream.TotalBytesRead, hashStream.FinaliseHash(), cancellationToken);

                return fileVersionId.Value.FileId;
            }
            catch
            {
                // Delete the file.
                if (fileVersionId != null)
                {
                    await _messageBus.Send(new FaasDeleteFileVersionMessageV1
                    {
                        Container = Name,
                        DateCreatedUtc = DateTime.UtcNow,
                        FileId = fileVersionId.Value.FileId,
                        VersionId = fileVersionId.Value.VersionId
                    });
                }

                // Something has gone wrong, so clean up the catalogue.
                if (fileVersionId != null)
                    await _catalogue.CancelWritingAsync(fileVersionId.Value.FileId, fileVersionId.Value.VersionId, cancellationToken);
                
                throw;
            }
        }

        /// <summary>
        /// Reads a file.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="id">The id.</param>
        /// <exception cref="FaasFileNotFoundException">The file was not found.</exception>
        /// /// <exception cref="FaasFileVersionNotFoundException">The file version was not found.</exception>
        /// <returns>The contents.</returns>
        public async Task<Stream> ReadAsync(Guid id, Guid? versionId, CancellationToken cancellationToken)
        {
            var header = await _catalogue.GetAsync(id, cancellationToken);
            if (header == null)
                throw new FaasFileNotFoundException();

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            FaasFileHeaderVersion? version;
            if (versionId != null)
                version = header.Versions.FirstOrDefault(v => v.VersionId == versionId);
            else
                version = header.Versions.First(v => v.VersionId == header.VersionId);

            if (version == null)
                throw new FaasFileVersionNotFoundException();

            return await _fileStore.ReadAsync(version.VersionId, cancellationToken);
        }
        
        public ValueTask<FaasFileHeader?> GetHeaderAsync(Guid fileId, CancellationToken cancellationToken)
        {
            return _catalogue.GetAsync(fileId, cancellationToken);
        }

        public  ValueTask<IEnumerable<FaasFileHeader>> ListHeadersAsync(int pageNumber, CancellationToken cancellationToken)
        {
            return _catalogue.ListAsync(pageNumber, cancellationToken);
        }

        public async Task<bool> HasFileAsync(Guid id, Guid? versionId, CancellationToken cancellationToken)
        {
            var header = await _catalogue.GetAsync(id, cancellationToken);
            if (header == null)
                return false;

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            FaasFileHeaderVersion? version;
            if (versionId != null)
                version = header.Versions.FirstOrDefault(v => v.VersionId == versionId);
            else
                version = header.Versions.First(v => v.VersionId == header.VersionId);

            return version != null;
        }

        public async Task DeleteFileAsync(Guid id, Guid? versionId, CancellationToken cancellationToken)
        {
            var header = await _catalogue.GetAsync(id, cancellationToken);
            if (header == null)
                throw new FaasFileNotFoundException();

            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            FaasFileHeaderVersion? version;
            if (versionId != null)
                version = header.Versions.FirstOrDefault(v => v.VersionId == versionId);
            else
                version = header.Versions.First(v => v.VersionId == header.VersionId);

            if (version == null)
                throw new FaasFileVersionNotFoundException();

            await _fileStore.DeleteAsync(version.VersionId, cancellationToken);
        }
    }
}