using System;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.InMemory;
using NUnit.Framework;

namespace FilesAsAService.UnitTests.InMemory
{
    [TestFixture]
    public class InMemoryFileStoreTests
    {
        private TestDataGenerator _testDataGenerator = new TestDataGenerator();

        private IFaasFileStore CreateFileStore()
        {
            return new InMemoryFaasFileStore();
        }

        [Test]
        public async Task DoesReadAsyncGetFile()
        {
            await using var inputStream = _testDataGenerator.CreateRandomStream(1024);

            using var fileStore = CreateFileStore();
            var id = Guid.NewGuid();

            await fileStore.CreateAsync(id, inputStream, CancellationToken.None);
            inputStream.Position = 0;
            
            using var readStream = await fileStore.ReadAsync(id, CancellationToken.None);
            Assert.IsNotNull(readStream);
            
            Assert.AreEqual(inputStream, readStream);
        }
        
         [Test]
        public async Task DoesCreateThrowWhenIdExists()
        {
            await using var inputStream = _testDataGenerator.CreateRandomStream(1024);

            using var fileStore = CreateFileStore();
            var id = Guid.NewGuid();

            await fileStore.CreateAsync(id, inputStream, CancellationToken.None);
            inputStream.Position = 0;

            Assert.ThrowsAsync<FaasFileExistsException>(async () =>
            {
                await fileStore.CreateAsync(id, inputStream, CancellationToken.None);
            });
        }
        
        [Test]
        public void DoesDoesReadAsyncThrowForInvalidIdGetFile()
        {
            var fileStore = CreateFileStore();
            var id = Guid.NewGuid();

            Assert.ThrowsAsync<FaasFileNotFoundException>(async () =>
            {
                await using var readStream = await fileStore.ReadAsync(id, CancellationToken.None);
            });
        }
    }
}