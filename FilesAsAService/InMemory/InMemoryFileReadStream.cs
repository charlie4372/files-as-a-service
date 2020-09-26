using System;
using System.IO;

namespace FilesAsAService.InMemory
{
    /// <summary>
    /// A stream for reading InMemoryFiles.
    /// </summary>
    public class InMemoryFileReadStream : Stream
    {
        /// <summary>
        /// The file to be streamed.
        /// </summary>
        private readonly InMemoryFile _file;

        /// <summary>
        /// The current block being read.
        /// </summary>
        private int _blockIndex;

        /// <summary>
        /// The offset within the current block.
        /// </summary>
        private int _blockOffset;

        /// <summary>
        /// The position within the file.
        /// </summary>
        private long _position;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="file">The file to be streamed.</param>
        public InMemoryFileReadStream(InMemoryFile file)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _blockIndex = 0;
            _blockOffset = 0;
            _position = 0;
        }
        
        /// <inheritdoc cref="Flush"/>
        public override void Flush()
        {
        }

        /// <inheritdoc cref="Read"/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var totalCopied = 0;
            var copiedThisIteration = -1;
            while (copiedThisIteration != 0)
            {
                // Read was can be read from the current block.
                // Keep reparting until the the stream is exhausted, or the request has been satisfied.
                copiedThisIteration = ReadFromBlock(buffer, offset + totalCopied, count - totalCopied);
                totalCopied += copiedThisIteration;
            }

            return totalCopied;
        }

        /// <summary>
        /// Reads was can be read from the current block.
        /// </summary>
        /// <param name="buffer">The buffer to store the dat in.</param>
        /// <param name="offset">The offset of the buffer to write to.</param>
        /// <param name="count">The number of bytes to write,</param>
        /// <returns>The number of bytes written.</returns>
        private int ReadFromBlock(byte[] buffer, int offset, int count)
        {
            if (_blockIndex >= _file.Blocks.Count)
                return 0;
            
            var block = _file.Blocks[_blockIndex];
            
            var bytesToCopy = Math.Min(block.Length - _blockOffset, count);
            Array.Copy(block, _blockOffset, buffer, offset, bytesToCopy);

            Seek(bytesToCopy, SeekOrigin.Current);
            
            return bytesToCopy;
        } 

        /// <inheritdoc cref="Seek"/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            // If we are starting at the beginning, move the position to the start and perform normal seeking.
            if (origin == SeekOrigin.Begin)
            {
                _position = 0;
                _blockIndex = 0;
                _blockOffset = 0;
                return Seek(offset, SeekOrigin.Current);
            }
            // If seeking from the end, move the position to the end and perform normal seeking.
            if (origin == SeekOrigin.End)
            {
                _position = Length;
                _blockIndex = _file.Blocks.Count;
                _blockOffset = _file.Blocks[_blockIndex].Length; // TODO this might be too far in the stream. Might have to come back to the last byte.
                return Seek(offset, SeekOrigin.Current);
            }

            if (offset > 0)
            {
                // If we are past the end of the blocks, leave.
                if (_blockIndex >= _file.Blocks.Count)
                    return _position;
                
                // Keep track of how many bytes left to seek.
                var offsetRemaining = offset;
                // Keep seeking while there is offsetRemaining, and there are seekable blocks.
                while (offsetRemaining > 0 && _position < Length)
                {
                    var block = _file.Blocks[_blockIndex];
                    // Determine how much of the offsetRemaining can be applied to the current block.
                    var offsetToApply = (int)Math.Min(offsetRemaining, block.Length - _blockOffset);
                    offsetRemaining -= offsetToApply;
                    _position += offsetToApply;
                    _blockOffset += offsetToApply;

                    // If this block is exhausted, advance to the next block.
                    if (_blockOffset == _file.Blocks[_blockIndex].Length)
                    {
                        // If this is the last block, leave.
                        if (_blockIndex == _file.Blocks.Count)
                            return _position;

                        // Advance the page and reset the block offset.
                        _blockIndex++;
                        _blockOffset = 0;
                    }
                }
            }
            if (offset < 0)
            {
                // Keep track of how many bytes left to seek.
                var offsetRemaining = offset;
                // Keep seeking while there is offsetRemaining, and there are seekable blocks.
                while (offsetRemaining > 0 && _position > 0)
                {
                    // If we are at the beginning of this block, load the previous block.
                    // The offset will point past the end.
                    if (_blockOffset == 0)
                    {
                        _blockIndex--;
                        _blockOffset = _file.Blocks[_blockIndex].Length;
                    }
                    
                    var offsetToApply = Math.Min(offsetRemaining, _blockOffset);
                    offsetRemaining -= offsetToApply;
                    _position += offsetToApply;
                }
                
                // If this block is exhausted, advance to the next block.
                if (_blockOffset == _file.Blocks[_blockIndex].Length)
                {
                    // If this is the last block, leave.
                    if (_blockIndex == _file.Blocks.Count)
                        return _position;

                    // Advance the page and reset the block offset.
                    _blockIndex++;
                    _blockOffset = 0;
                }
            }

            return _position;
        }

        /// <inheritdoc cref="SetLength"/>
        public override void SetLength(long value)
        {
            throw new System.NotSupportedException();
        }

        /// <inheritdoc cref="Write"/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotSupportedException();
        }

        /// <inheritdoc cref="Read"/>
        public override bool CanRead => true;
        
        /// <inheritdoc cref="CanSeek"/>
        /// <remarks>
        /// Seeking was only enabled for tests.
        /// </remarks>>
        public override bool CanSeek => true;
        
        /// <inheritdoc cref="CanWrite"/>
        public override bool CanWrite => false;
        
        /// <inheritdoc cref="Length"/>
        public override long Length => _file.Length;
        
        /// <inheritdoc cref="Position"/>
        public override long Position
        {
            get => _position;
            set
            {
                if (value == 0)
                    Seek(0, SeekOrigin.Begin);
                else
                    Seek(value - _position, SeekOrigin.Current);
            }
        }
    }
}