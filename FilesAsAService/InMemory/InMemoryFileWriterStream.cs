using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace FilesAsAService.InMemory
{
    /// <summary>
    /// A stream for writing InMemoryFiles.
    /// </summary>
    public class InMemoryFileWriterStream : Stream
    {
        /// <summary>
        /// The block size to use.
        /// </summary>
        private readonly int _blockSize;

        /// <summary>
        /// The contents.
        /// </summary>
        private readonly List<byte[]> _blocks;

        /// <summary>
        /// The current block.
        /// </summary>
        private byte[]? _currentBlock;
        
        /// <summary>
        /// The offset within the current block.
        /// </summary>
        private int _blockOffset;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="blockSize">the size of the block.</param>
        public InMemoryFileWriterStream(int blockSize)
        {
            _blockSize = blockSize;
            _blocks = new List<byte[]>();
            _blockOffset = 0;
        }
        
        /// <inheritdoc cref="Flush"/>
        public override void Flush()
        {
        }

        /// <inheritdoc cref="Read"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="Seek"/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="SetLength"/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="Write"/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            var bytesWritten = 0;
            while (bytesWritten != count)
            {
                // Keep writing until all the data is written.
                bytesWritten += WriteToBlock(buffer, offset + bytesWritten, count - bytesWritten);
            }
        }

        /// <summary>
        /// Write to the current block.
        /// </summary>
        /// <param name="buffer">The buffer to read from.</param>
        /// <param name="offset">The offset in the buffer to read from.</param>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The number of bytes written.</returns>
        private int WriteToBlock(byte[] buffer, int offset, int count)
        {
            if (_currentBlock == null || _blockOffset == _blockSize)
            {
                _currentBlock = ArrayPool<byte>.Shared.Rent(_blockSize);
                _blocks.Add(_currentBlock);
                _blockOffset = 0;
            }

            // Write what can eb written to the block.
            var bytesAvailableInBlock = _blockSize - _blockOffset;
            var bytesToWrite = Math.Min(count, bytesAvailableInBlock);
            Array.Copy(buffer, offset, _currentBlock, _blockOffset, bytesToWrite);
            _blockOffset += bytesToWrite;
            return bytesToWrite;
        }

        /// <inheritdoc cref="CanRead"/>
        public override bool CanRead => false;
        
        /// <inheritdoc cref="CanSeek"/>
        public override bool CanSeek => false;
        
        /// <inheritdoc cref="Flush"/>
        public override bool CanWrite => true;
        
        /// <inheritdoc cref="Length"/>
        public override long Length => (_blocks.Count > 1 ? (_blocks.Count - 1 * _blockSize) : 0) + _blockOffset;

        /// <inheritdoc cref="Position"/>
        public override long Position
        {
            get => Length;
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the blocks holding the content.
        /// </summary>
        public IReadOnlyList<byte[]> GetBlocks()
        {
            return _blocks;
        }
    }
}