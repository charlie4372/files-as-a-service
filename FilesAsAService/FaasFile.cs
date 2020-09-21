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
        /// Appends a stream to the file.
        /// </summary>
        /// <returns>The number of bytes appended.</returns>
        public async Task WriteAsync(Stream stream, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                await _fileStore.ReplaceAsync(_header.Id, stream, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
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