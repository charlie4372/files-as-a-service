using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.MessageBus;

namespace FilesAsAService
{
    /// <summary>
    /// The container.
    /// </summary>
    public class FaasContainer : IDisposable
    {
        /// <summary>
        /// The catalogue.
        /// </summary>
        private IFaasCatalogue? _catalogue;
        
        /// <summary>
        /// The store.
        /// </summary>
        private IFaasFileStore? _fileStore;

        /// <summary>
        /// The message bus.
        /// </summary>
        private IFaasMessageBus? _messageBus;

        /// <summary>
        /// Sets the message bus.
        /// </summary>
        /// <param name="messageBus">The server.</param>
        public void SetMessageBus(IFaasMessageBus? messageBus)
        {
            if (messageBus == null)
                _messageBus = null;
            else if (_messageBus != null)
                throw new InvalidOperationException("Container already has a message bus.");
            else
                _messageBus = messageBus;
        }
        
        /// <summary>
        /// Adds a store to the container.
        /// </summary>
        /// <param name="fileStore">The file store.</param>
        public void AddStore(IFaasFileStore fileStore)
        {
            if (_fileStore != null) throw new InvalidOperationException("Container already has a store.");
            
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        }
        
        /// <summary>
        /// Adds a catalogue to the container.
        /// </summary>
        /// <param name="catalogue">The name in the server.</param>
        public void AddCatalogue(IFaasCatalogue catalogue)
        {
            if (_catalogue != null) throw new InvalidOperationException("Container already has a catalogue.");

            _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
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
            if (_fileStore == null)
                throw new InvalidOperationException("No store set.");
            if (_catalogue == null)
                throw new InvalidOperationException("No catalogue set.");
            
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
                if (_messageBus != null)
                {
                    if (fileVersionId != null)
                        await _messageBus.Send(new FaasDeleteFromStoreMessageV1(_fileStore.Name, fileVersionId.Value.VersionId));
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
            if (_fileStore == null)
                throw new InvalidOperationException("No store set.");
            if (_catalogue == null)
                throw new InvalidOperationException("No catalogue set.");
            
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
            if (_catalogue == null)
                throw new InvalidOperationException("No catalogue set.");
            return _catalogue.GetAsync(fileId, cancellationToken);
        }

        public  ValueTask<IEnumerable<FaasFileHeader>> ListHeadersAsync(int pageNumber, CancellationToken cancellationToken)
        {
            if (_catalogue == null)
                throw new InvalidOperationException("No catalogue set.");
            return _catalogue.ListAsync(pageNumber, cancellationToken);
        }

        /// <summary>
        /// The stores.
        /// </summary>
        public IEnumerable<IFaasFileStore> Stores => _fileStore == null ? new IFaasFileStore[0] : new[] {_fileStore};

        /// <summary>
        /// The catalogues.
        /// </summary>
        public IEnumerable<IFaasCatalogue> Catalogues => _catalogue == null ? new IFaasCatalogue[0] : new[] {_catalogue};

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            _fileStore?.Dispose();
            
            GC.SuppressFinalize(this);
        }
    }
}