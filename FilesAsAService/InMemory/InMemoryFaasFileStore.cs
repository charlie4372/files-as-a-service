using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.InMemory
{
    public class InMemoryFaasFileStore : IFaasFileStore
    {
        private readonly Dictionary<Guid, LockableByteArray> _files = new Dictionary<Guid, LockableByteArray>();

        private readonly Semaphore _lock = new Semaphore(1, 1);

        public async Task CreateAsync(Guid fileId, Stream stream, CancellationToken cancellationToken)
        {
            LockableByteArray fileData;
            
            _lock.WaitOne();
            try
            {
                if (_files.ContainsKey(fileId))
                    throw new FaasFileExistsException();

                fileData = new LockableByteArray();

                _files.Add(fileId, fileData);
            }
            finally
            {
                _lock.Release();
            }
            
            fileData.WaitOne();
            try
            {
                await using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer, cancellationToken);
                await buffer.FlushAsync(cancellationToken);
                fileData.Data = buffer.ToArray();
            }
            finally
            {
                fileData.Release();
            }
        }

        public Task<Stream> ReadAsync(Guid fileId, CancellationToken cancellationToken)
        {
            LockableByteArray fileData;
            
            _lock.WaitOne();
            try
            {
                if (!_files.ContainsKey(fileId))
                    throw new FaasFileNotFoundException();

                fileData = _files[fileId];
            }
            finally
            {
                _lock.Release();
            }
            
            fileData.WaitOne();
            try
            {
                var wrappedStream = new FaasWrappedStream(new MemoryStream(fileData.Data));
                wrappedStream.Disposing += (sender, args) => fileData.Release(); 
                return Task.FromResult<Stream>(wrappedStream);
            }
            catch
            {
                fileData.Release();
                throw;
            }
        }
    }
}