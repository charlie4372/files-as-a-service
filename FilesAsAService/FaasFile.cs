using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    public class FaasFile
    {
        private readonly IFaasFileStore _fileStore;

        private readonly FaasFileHeader _header;
        
        private readonly Semaphore _lock = new Semaphore(1, 1);

        public FaasFile(IFaasFileStore fileStore, FaasFileHeader header)
        {
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
            _header = header ?? throw new ArgumentNullException(nameof(header));
        }

        /// <summary>
        /// Reads a 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Stream> ReadAsync(CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                return await _fileStore.ReadAsync(_header.Id, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}