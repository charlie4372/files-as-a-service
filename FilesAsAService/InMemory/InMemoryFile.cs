using System.Collections.Generic;
using System.Threading;

namespace FilesAsAService.InMemory
{
    /// <summary>
    /// Represents a file stored in memory.
    /// </summary>
    public class InMemoryFile
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
        /// Sets the file contents.
        /// </summary>
        /// <param name="data">The file contents.</param>
        /// <param name="length">The file length.</param>
        public void SetData(IReadOnlyList<byte[]> data, long length)
        {
            _blocks = data;
            Length = length;
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
    }
}