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
        private readonly int _blockSize;
        
        /// <summary>
        /// The files.
        /// </summary>
        private readonly Dictionary<Guid, InMemoryFile> _files = new Dictionary<Guid, InMemoryFile>();

        /// <summary>
        /// Lock to protect <see cref="_files"/>.
        /// </summary>
        private readonly Semaphore _lock = new Semaphore(1, 1);

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="name">The name. The message bus will use this to locate it.</param>
        /// <param name="blockSize">The block size.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public InMemoryFaasFileStore(string name, int blockSize = 1024)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            if (blockSize < 0) throw new ArgumentOutOfRangeException(nameof(blockSize));

            _blockSize = blockSize;
        }

        /// <inheritdoc cref="Name"/>
        public string Name { get; }

        /// <inheritdoc cref="ContainsAsync"/>
        public Task<bool> ContainsAsync(Guid id, CancellationToken cancellationToken)
        {
            // Lock the db.
            _lock.WaitOne();
            try
            {
                return Task.FromResult(_files.ContainsKey(id));
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc cref="CreateAsync"/>
        public async Task CreateAsync(Guid id, Stream stream, CancellationToken cancellationToken)
        {
            InMemoryFile fileData;
            
            // Lock the db.
            _lock.WaitOne();
            try
            {
                // Throw if the file isn't found.
                if (_files.ContainsKey(id))
                    throw new FaasFileExistsException();

                // Add the LockableByteArray that will hold the data.
                fileData = new InMemoryFile();
                
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
                await using var buffer = new InMemoryFileWriterStream(_blockSize);
                await stream.CopyToAsync(buffer, cancellationToken);
                await buffer.FlushAsync(cancellationToken);
                fileData.SetData(buffer.GetBlocks(), buffer.Length, _blockSize);
            }
            finally
            {
                fileData.Release();
            }
        }

        /// <inheritdoc cref="ReadAsync"/>
        public Task<Stream> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            InMemoryFile fileData;
            
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
                return Task.FromResult<Stream>(new InMemoryFileReadStream(fileData));
            }
            finally
            {
                fileData.Release();
            }
        }
        
        /// <inheritdoc cref="DeleteAsync"/>
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                // Throw is the file doesn't exist.
                if (!_files.ContainsKey(id))
                    throw new FaasFileNotFoundException();

                _files.Remove(id);
            }
            finally
            {
                _lock.Release();
            }
            
            return Task.CompletedTask;
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

                foreach (var kvp in _files)
                {
                    kvp.Value.Dispose();
                }
                
                _files.Clear();
            }
        }
    }
}