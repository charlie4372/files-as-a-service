using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    public class FaasContainer
    {
        private readonly IFaasCatalogue _catalogue;
        private readonly IFaasFileStore _fileStore;
        
        public FaasContainer(IFaasCatalogue catalogue, IFaasFileStore fileStore)
        {
            _catalogue = catalogue ?? throw new ArgumentNullException(nameof(catalogue));
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        }

        public async Task CreateAsync(string filename, Stream stream, CancellationToken cancellationToken)
        {
            FaasFileHeader? header = null;
            try
            {
                header = await _catalogue.StartCreateAsync(filename, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                await using var inputByteCounterStream = new ByteCounterStreamProxy(stream, false);
                await using var hashStream = new HashGeneratorStreamProxy(inputByteCounterStream, false);

                await _fileStore.CreateAsync(header.Id, inputByteCounterStream, cancellationToken);

                await _catalogue.CompleteCreateAsync(header.Id, inputByteCounterStream.TotalBytesRead, hashStream.FinaliseHash(), cancellationToken); 
            }
            catch
            {
                if (header != null)
                    await _catalogue.CancelCreateAsync(header.Id, cancellationToken);
                
                throw;
            }
        }
    }
}