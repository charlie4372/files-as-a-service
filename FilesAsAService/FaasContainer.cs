using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
        
        public FaasContainer(IFaasCatalogue catalogue, IFaasFileStore fileStore)
        {
            _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
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
                await _fileStore.CreateAsync(fileVersionId.Value.FileId, inputByteCounterStream, cancellationToken);

                // Complete the file and make it available.
                await _catalogue.CompleteWritingAsync(fileVersionId.Value.FileId, fileVersionId.Value.VersionId,  inputByteCounterStream.TotalBytesRead, hashStream.FinaliseHash(), cancellationToken);

                return fileVersionId.Value.FileId;
            }
            catch
            {
                // TODO delete the file contents.
                
                // Something has gone wrong, so clean up the file.
                if (fileVersionId != null)
                    await _catalogue.CancelWritingAsync(fileVersionId.Value.FileId, fileVersionId.Value.VersionId, cancellationToken);
                
                throw;
            }
        }
    }
}