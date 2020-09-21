using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FilesAsAService.UnitTests
{
    [TestFixture]
    public class HashGeneratorStreamProxyTests
    {
        private TestDataGenerator _testDataGenerator = new TestDataGenerator();
        
        [TestCase(0, 100)]
        [TestCase(1, 100)]
        [TestCase(100, 100)]
        [TestCase(101, 100)]
        [TestCase(201, 100)]
        [TestCase(299, 100)]
        [TestCase(300, 100)]
        public async Task DoesReadAsyncCountNumberOfBytes(int size, int blockSize)
        {
            using var input = _testDataGenerator.CreateRandomStream(size);
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
            using var input = _testDataGenerator.CreateRandomStream(size);
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
    }
}