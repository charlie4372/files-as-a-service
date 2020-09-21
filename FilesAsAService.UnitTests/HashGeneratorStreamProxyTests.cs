using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FilesAsAService.UnitTests
{
    [TestFixture]
    public class HashGeneratorStreamProxyTests
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
            using var controlHashGenerator = new SHA512Managed();

            var controlHash = controlHashGenerator.ComputeHash(input);

            input.Position = 0;
            using var hashGeneratorStreamProxy = new HashGeneratorStreamProxy(input, false);

            var buffer = new byte[blockSize];
            var bytesRead = await hashGeneratorStreamProxy.ReadAsync(buffer,  0, buffer.Length);
            while (bytesRead != 0)
            {
                bytesRead = await hashGeneratorStreamProxy.ReadAsync(buffer, 0, buffer.Length);
            }

            var hash = hashGeneratorStreamProxy.FinaliseHash();

            Assert.AreEqual(controlHash, hash);
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
            using var controlHashGenerator = new SHA512Managed();

            var controlHash = controlHashGenerator.ComputeHash(input);

            input.Position = 0;
            using var hashGeneratorStreamProxy = new HashGeneratorStreamProxy(input, false);

            var buffer = new byte[blockSize];
            var bytesRead = hashGeneratorStreamProxy.Read(buffer,  0, buffer.Length);
            while (bytesRead != 0)
            {
                bytesRead = hashGeneratorStreamProxy.Read(buffer, 0, buffer.Length);
            }

            var hash = hashGeneratorStreamProxy.FinaliseHash();

            Assert.AreEqual(controlHash, hash);
        }
    
        private Stream CreateRandomStream(int size)
        {
            var buffer = new byte[size];
            _random.NextBytes(buffer);

            return new MemoryStream(buffer);
        }   
    }
}