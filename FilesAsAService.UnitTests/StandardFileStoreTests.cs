using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FilesAsAService.UnitTests
{
    public abstract class StandardFileStoreTests
    {
        private readonly TestDataGenerator _testDataGenerator = new TestDataGenerator();

        protected abstract IFaasFileStore CreateFileStore();

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
        public async Task DoesContainAsyncFindFile()
        {
            await using var inputStream = _testDataGenerator.CreateRandomStream(1024);

            using var fileStore = CreateFileStore();
            var id = Guid.NewGuid();

            await fileStore.CreateAsync(id, inputStream, CancellationToken.None);
            inputStream.Position = 0;
            
            var found = await fileStore.ContainsAsync(id, CancellationToken.None);
            Assert.IsTrue(found);
        }
        
        [Test]
        public async Task DoesContainAsyncNotFindFile()
        {
            using var fileStore = CreateFileStore();
            var id = Guid.NewGuid();

            var found = await fileStore.ContainsAsync(id, CancellationToken.None);
            Assert.IsFalse(found);
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
        public void DoesReadAsyncThrowForInvalidIdGetFile()
        {
            var fileStore = CreateFileStore();
            var id = Guid.NewGuid();

            Assert.ThrowsAsync<FaasFileNotFoundException>(async () =>
            {
                await using var readStream = await fileStore.ReadAsync(id, CancellationToken.None);
            });
        }
        
        [Test]
        public async Task DoesDeleteAsyncRemoveFile()
        {
            await using var inputStream = _testDataGenerator.CreateRandomStream(1024);

            using var fileStore = CreateFileStore();
            var id = Guid.NewGuid();

            await fileStore.CreateAsync(id, inputStream, CancellationToken.None);
            inputStream.Position = 0;
            
            await fileStore.DeleteAsync(id, CancellationToken.None);

            var exists = await fileStore.ContainsAsync(id, CancellationToken.None);
            Assert.IsFalse(exists);
        }
        
        [Test]
        public void DoesDeleteAsyncThrowForInvalidId()
        {
            var fileStore = CreateFileStore();
            var id = Guid.NewGuid();

            Assert.ThrowsAsync<FaasFileNotFoundException>(async () =>
            {
                await fileStore.DeleteAsync(id, CancellationToken.None);
            });
        }

    }
}