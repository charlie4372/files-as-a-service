using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.InMemory
{
    /// <summary>
    /// Stores the files in memory.
    /// </summary>
    public class InMemoryFaasFileStore : IFaasFileStore
    {
        /// <summary>
        /// The files.
        /// </summary>
        private readonly Dictionary<Guid, LockableByteArray> _files = new Dictionary<Guid, LockableByteArray>();

        /// <summary>
        /// Lock to protect <see cref="_files"/>.
        /// </summary>
        private readonly Semaphore _lock = new Semaphore(1, 1);

        /// <inheritdoc cref="CreateAsync"/>
        public async Task CreateAsync(Guid id, Stream stream, CancellationToken cancellationToken)
        {
            LockableByteArray fileData;
            
            // Lock the db.
            _lock.WaitOne();
            try
            {
                // Throw if the file isn't found.
                if (_files.ContainsKey(id))
                    throw new FaasFileExistsException();

                // Add the LockableByteArray that will hold the data.
                fileData = new LockableByteArray();
                
                _files.Add(id, fileData);
            }
            finally
            {
                _lock.Release();
            }
            
            // Lock the file until the writing is complete.
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

        /// <inheritdoc cref="ReadAsync"/>
        public Task<Stream> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            LockableByteArray fileData;
            
            _lock.WaitOne();
            try
            {
                // Throw is the file doesn't exist.
                if (!_files.ContainsKey(id))
                    throw new FaasFileNotFoundException();

                fileData = _files[id];
            }
            finally
            {
                _lock.Release();
            }
            
            // Lock the file while its being read.
            fileData.WaitOne();
            try
            {
                return Task.FromResult<Stream>(new MemoryStream(fileData.Data));
            }
            finally
            {
                fileData.Release();
            }
        }

        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lock.Dispose();
                
                _files.Clear();
            }
        }
    }
}