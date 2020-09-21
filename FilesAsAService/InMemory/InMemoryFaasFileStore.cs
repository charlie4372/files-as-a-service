using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.InMemory
{
    public class InMemoryFaasFileStore : IFaasFileStore
    {
        private readonly Dictionary<Guid, LockableMemoryStream> _files = new Dictionary<Guid, LockableMemoryStream>();

        private readonly Semaphore _lock = new Semaphore(1, 1);

        public async Task CreateAsync(Guid fileId, Stream stream, CancellationToken cancellationToken)
        {
            LockableMemoryStream fileStream;
            
            _lock.WaitOne();
            try
            {
                if (_files.ContainsKey(fileId))
                    throw new FaasFileExistsException();

                fileStream = new LockableMemoryStream();

                _files.Add(fileId, fileStream);
            }
            finally
            {
                _lock.Release();
            }
            
            fileStream.WaitOne();
            try
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
            finally
            {
                fileStream.Release();
            }
        }

        public Task<Stream> ReadAsync(Guid fileId, CancellationToken cancellationToken)
        {
            LockableMemoryStream fileStream;
            
            _lock.WaitOne();
            try
            {
                if (!_files.ContainsKey(fileId))
                    throw new FaasFileNotFoundException();

                fileStream = _files[fileId];
            }
            finally
            {
                _lock.Release();
            }
            
            fileStream.WaitOne();
            try
            {
                var data = fileStream.ToArray();
                var wrappedStream = new FaasWrappedStream(new MemoryStream(data));
                wrappedStream.Disposing += (sender, args) => fileStream.Release(); 
                return Task.FromResult<Stream>(wrappedStream);
            }
            catch
            {
                fileStream.Release();
                throw;
            }
        }
    }
}