using System;

namespace FilesAsAService.UnitTests.Extensions
{
    public static class ByteExtensions
    {
        /// <summary>
        /// Randomises the data in a byte array.
        /// </summary>
        public static void Randomise(this byte[] value, int? seed = null)
        {
            var random = seed != null ? new Random(seed.Value) : new Random();
            random.NextBytes(value);
        }
    }
}