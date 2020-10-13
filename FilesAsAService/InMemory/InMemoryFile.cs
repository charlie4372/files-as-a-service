using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace FilesAsAService.InMemory
{
    /// <summary>
    /// Represents a file stored in memory.
    /// </summary>
    public class InMemoryFile : IDisposable
    {
        /// <summary>
        /// A common reference to empty data.
        /// </summary>
        private static readonly IList<byte[]> EmptyData = new List<byte[]>();

        /// <summary>
        /// The blocks that make up the file.
        /// </summary>
        private IReadOnlyList<byte[]> _blocks = (IReadOnlyList<byte[]>)EmptyData;

        /// <summary>
        /// A concurrency lock.
        /// </summary>
        private readonly Semaphore _lock = new Semaphore(1, 1);
        
        /// <summary>
        /// The length of the file.
        /// </summary>
        public long Length { get; private set; }
        
        /// <summary>
        /// The size of the blocks.
        /// </summary>
        public int BlockSize { get; private set; }

        /// <summary>
        /// Sets the file contents.
        /// </summary>
        /// <param name="data">The file contents.</param>
        /// <param name="length">The file length.</param>
        /// <param name="blockSize">The size of the blocks.</param>
        public void SetData(IReadOnlyList<byte[]> data, long length, int blockSize)
        {
            _blocks = data;
            Length = length;
            BlockSize = blockSize;
        }

        /// <summary>
        /// Waits for a lock on the resource.
        /// </summary>
        public void WaitOne()
        {
            _lock.WaitOne();
        }

        /// <summary>
        /// Releases a lock on the resource.
        /// </summary>
        public void Release()
        {
            _lock.Release();
        }

        /// <summary>
        /// The contents of the file.
        /// </summary>
        public IReadOnlyList<byte[]> Blocks => _blocks;
        
        // <inheritdoc cref="Dispose"/>
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

                foreach (var block in _blocks)
                {
                    if (block != null)
                        ArrayPool<byte>.Shared.Return(block);
                }
            }
        }
    }
}