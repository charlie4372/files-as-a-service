using System;
using System.IO;

namespace FilesAsAService.UnitTests
{
    public class TestDataGenerator
    {
        private readonly Random _random = new Random();

        public byte[] CreateRandomByteArray(int size)
        {
            var buffer = new byte[size];
            _random.NextBytes(buffer);

            return buffer;
        }
        
        public Stream CreateRandomStream(int size)
        {
            return new MemoryStream(CreateRandomByteArray(size));
        }
    }
}