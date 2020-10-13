using System;

namespace FilesAsAService.Helpers
{
    public class BlockHelper
    {
        /// <summary>
        /// Calculates the number of blocks in the version.
        /// </summary>
        public static int CalculateNumberOfBlocks(long length, int blockSize)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            if (blockSize < 0) throw new ArgumentOutOfRangeException(nameof(blockSize));
            
            if (blockSize == 0)
                return 1;
            
            return (int)((length + (blockSize - 1)) / blockSize);
        }

        /// <summary>
        /// Gets the size of a given block.
        /// </summary>
        /// <param name="blockNumber">The block number.</param>
        /// <param name="blockSize">The size of the blocks.</param>
        /// <param name="length">The length of the data.</param>
        /// <returns>The size of the block.</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static int GetBlockSize(int blockNumber, long length, int blockSize)
        {
            var numberOfBlocks = CalculateNumberOfBlocks(length, blockSize);
            
            if (blockNumber < 0) throw new ArgumentOutOfRangeException(nameof(blockNumber));
            if (blockNumber > numberOfBlocks) throw new ArgumentNullException(nameof(blockNumber));
            
            return numberOfBlocks == 1 ? (int)length
                : blockNumber < numberOfBlocks - 1 ? blockSize
                : (int)(length - (blockSize * blockNumber - 1));
        }
    }
}