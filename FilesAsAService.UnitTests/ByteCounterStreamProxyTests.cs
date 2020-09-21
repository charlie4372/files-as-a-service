using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FilesAsAService.UnitTests
{
    [TestFixture]
    public class ByteCounterStreamProxyTests
    {
        private Random _random = new Random();

        [TestCase(0, 100)]
        [TestCase(1, 100)]
        [TestCase(100, 100)]
        [TestCase(101, 100)]
        [TestCase(201, 100)]
        [TestCase(299, 100)]
        [TestCase(300, 100)]
        public async Task DoesReadAsyncCountNumberOfBytes(int size, int blockSize)
        {
            using var input = CreateRandomStream(size);
            using var byteCounterStreamProxy = new ByteCounterStreamProxy(input, false);

            var buffer = new byte[blockSize];
            var totalBytesRead = 0;
            var bytesRead = await byteCounterStreamProxy.ReadAsync(buffer,  0, buffer.Length);
            totalBytesRead += bytesRead;
            while (bytesRead != 0)
            {
                bytesRead = await byteCounterStreamProxy.ReadAsync(buffer, 0, buffer.Length);
                totalBytesRead += bytesRead;
            }

            Assert.AreEqual(size, totalBytesRead);
        }
        
        [TestCase(0, 100)]
        [TestCase(1, 100)]
        [TestCase(100, 100)]
        [TestCase(101, 100)]
        [TestCase(201, 100)]
        [TestCase(299, 100)]
        [TestCase(300, 100)]
        public void DoesReadCountNumberOfBytes(int size, int blockSize)
        {
            using var input = CreateRandomStream(size);
            using var byteCounterStreamProxy = new ByteCounterStreamProxy(input, false);

            var buffer = new byte[blockSize];
            var totalBytesRead = 0;
            var bytesRead = byteCounterStreamProxy.Read(buffer, 0, buffer.Length);
            totalBytesRead += bytesRead;
            while (bytesRead != 0)
            {
                bytesRead = byteCounterStreamProxy.Read(buffer, 0, buffer.Length);
                totalBytesRead += bytesRead;
            }

            Assert.AreEqual(size, totalBytesRead);
        }
    
        private Stream CreateRandomStream(int size)
        {
            var buffer = new byte[size];
            _random.NextBytes(buffer);

            return new MemoryStream(buffer);
        }   
    }
}