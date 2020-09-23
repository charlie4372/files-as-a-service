using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.InMemory;
using NUnit.Framework;

namespace FilesAsAService.UnitTests.InMemory
{
    [TestFixture]
    public class InMemoryFaasCatalogueTests
    {
        private readonly TestDataGenerator _testDataGenerator = new TestDataGenerator();
            
        private IFaasCatalogue CreateCatalogue()
        {
            return new InMemoryFaasCatalogue();
        }
        
        [Test]
        public async Task DoesStartCreateAsyncStartCreating()
        {
            var catalogue = CreateCatalogue();

            var fileVersionId = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);
            var header = await catalogue.GetAsync(fileVersionId.FileId, CancellationToken.None);
            Assert.IsNotNull(header);
            Assert.AreNotEqual(Guid.Empty, header.Id);
            Assert.AreEqual("test.txt", header.Name);
            Assert.IsTrue(header.DateCreatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsNull(header.DateDeletedUtc);
            Assert.AreEqual(1, header.Versions.Length);

            var version = header.Versions.FirstOrDefault(v => v.Id == fileVersionId.VersionId);
            Assert.IsNotNull(version);
            Assert.AreEqual(0, version.Length);
            Assert.IsTrue(version.DateCreatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsNull(version.DateDeletedUtc);
            Assert.AreEqual(true, version.Writing);
        }
        
        [Test]
        public async Task DoesStartGetAsyncReturnNullWhenNotFound()
        {
            var catalogue = CreateCatalogue();

            var header = await catalogue.GetAsync(Guid.NewGuid(), CancellationToken.None);
                            
            Assert.IsNull(header);
        }
        
        [Test]
        public async Task DoesCompleteWritingAsyncUpdateFields()
        {
            var catalogue = CreateCatalogue();

            var fileVersionId = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            var hash = _testDataGenerator.CreateRandomByteArray(16);
            await catalogue.CompleteWritingAsync(fileVersionId.FileId, fileVersionId.VersionId, 100, hash, CancellationToken.None);
            var header = await catalogue.GetAsync(fileVersionId.FileId, CancellationToken.None);
            Assert.IsNotNull(header);
            Assert.AreNotEqual(Guid.Empty, header.Id);
            Assert.AreEqual("test.txt", header.Name);
            Assert.IsTrue(header.DateCreatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsNull(header.DateDeletedUtc);

            var version = header.Versions.FirstOrDefault(v => v.Id == fileVersionId.VersionId);
            Assert.IsNotNull(version);
            Assert.AreEqual(100, version.Length);
            Assert.IsTrue(version.DateCreatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsNull(version.DateDeletedUtc);
            Assert.AreEqual(false, version.Writing);
        }
        
        [Test]
        public void DoesCompleteWritingAsyncThrowOnInvalidFileId()
        {
            var catalogue = CreateCatalogue();

            Assert.ThrowsAsync<FaasFileNotFoundException>(async () =>
            {
                var hash = _testDataGenerator.CreateRandomByteArray(16);
                await catalogue.CompleteWritingAsync(Guid.NewGuid(), Guid.NewGuid(), 100, hash, CancellationToken.None);
            });
        }
        
        [Test]
        public async Task DoesCompleteWritingAsyncThrowOnInvalidVersionId()
        {
            var catalogue = CreateCatalogue();

            var fileVersionId = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            Assert.ThrowsAsync<FaasFileVersionNotFoundException>(async () =>
            {
                var hash = _testDataGenerator.CreateRandomByteArray(16);
                await catalogue.CompleteWritingAsync(fileVersionId.FileId, Guid.NewGuid(), 100, hash, CancellationToken.None);
            });
        }
        
        [Test]
        public async Task DoesCompleteWritingAsyncThrowOnActiveHeader()
        {
            var catalogue = CreateCatalogue();

            var fileVersionId = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            var hash = _testDataGenerator.CreateRandomByteArray(16);
            await catalogue.CompleteWritingAsync(fileVersionId.FileId, fileVersionId.VersionId, 100, hash, CancellationToken.None);
            Assert.ThrowsAsync<FaasInvalidOperationException>(async () => { await catalogue.CompleteWritingAsync(fileVersionId.FileId, fileVersionId.VersionId, 100, hash, CancellationToken.None); });
        }

        [Test]
        public async Task DoesCancelWritingAsyncDeleteHeader()
        {
            var catalogue = CreateCatalogue();

            var fileVersionId = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            await catalogue.CancelWritingAsync(fileVersionId.FileId, fileVersionId.VersionId, CancellationToken.None);
            var header = await catalogue.GetAsync(fileVersionId.FileId, CancellationToken.None);
            Assert.IsNull(header);
        }

        [Test]
        public void DoesCancelWritingAsyncThrowOnInvalidFileId()
        {
            var catalogue = CreateCatalogue();

            Assert.ThrowsAsync<FaasFileNotFoundException>(async () => { await catalogue.CancelWritingAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None); });
        }

        [Test]
        public async Task DoesCancelWritingAsyncThrowOnInvalidVersionId()
        {
            var catalogue = CreateCatalogue();

            var fileVersionId = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            Assert.ThrowsAsync<FaasFileVersionNotFoundException>(async () => { await catalogue.CancelWritingAsync(fileVersionId.FileId, Guid.NewGuid(), CancellationToken.None); });
        }
        
        [Test]
        public async Task DoesCancelWritingAsyncThrowOnActiveHeader()
        {
            var catalogue = CreateCatalogue();

            var fileVersionId = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            var hash = _testDataGenerator.CreateRandomByteArray(16);
            await catalogue.CompleteWritingAsync(fileVersionId.FileId, fileVersionId.VersionId, 100, hash, CancellationToken.None);
            Assert.ThrowsAsync<FaasInvalidOperationException>(async () => { await catalogue.CancelWritingAsync(fileVersionId.FileId, fileVersionId.VersionId, CancellationToken.None); });
        }
    }
}