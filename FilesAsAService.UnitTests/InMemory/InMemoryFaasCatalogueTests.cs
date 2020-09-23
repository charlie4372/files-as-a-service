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
        private TestDataGenerator _testDataGenerator = new TestDataGenerator();
            
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
            await catalogue.CompleteWritingAsync(fileVersionId.Id, 100, hash, CancellationToken.None);
            var header = await catalogue.GetAsync(fileVersionId.Id, CancellationToken.None);
            Assert.IsNotNull(header);
            Assert.AreNotEqual(Guid.Empty, header.Id);
            Assert.AreEqual("test.txt", header.Name);
            Assert.AreEqual(100, header.Length);
            Assert.IsTrue(header.DateCreatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsTrue(header.DateUpdatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsNull(header.DateDeletedUtc);
            Assert.AreEqual(FaasFileHeaderStatus.Active, header.Status);
        }
        
        [Test]
        public void DoesCompleteCreateAsyncThrowOnInvalidId()
        {
            var catalogue = CreateCatalogue();

            Assert.ThrowsAsync<FaasFileNotFoundException>(async () =>
            {
                var hash = _testDataGenerator.CreateRandomByteArray(16);
                await catalogue.CompleteCreateAsync(Guid.NewGuid(), 100, hash, CancellationToken.None);
            });
        }
        
        [Test]
        public async Task DoesCompleteCreateAsyncThrowOnActiveHeader()
        {
            var catalogue = CreateCatalogue();

            var creatingHeader = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            var hash = _testDataGenerator.CreateRandomByteArray(16);
            await catalogue.CompleteCreateAsync(creatingHeader.Id, 100, hash, CancellationToken.None);
            Assert.ThrowsAsync<FaasInvalidOperationException>(async () => { await catalogue.CompleteCreateAsync(creatingHeader.Id, 100, hash, CancellationToken.None); });
        }

        [Test]
        public async Task DoesCancelCreateAsyncDeleteHeader()
        {
            var catalogue = CreateCatalogue();

            var creatingHeader = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            await catalogue.CancelWritingAsync(creatingHeader.Id, CancellationToken.None);
            var header = await catalogue.GetAsync(creatingHeader.Id, CancellationToken.None);
            Assert.IsNull(header);
        }

        [Test]
        public void DoesCancelCreateAsyncThrowOnInvalidId()
        {
            var catalogue = CreateCatalogue();

            Assert.ThrowsAsync<FaasFileNotFoundException>(async () => { await catalogue.CancelWritingAsync(Guid.NewGuid(), CancellationToken.None); });
        }
        
        [Test]
        public async Task DoesCancelCreateAsyncThrowOnActiveHeader()
        {
            var catalogue = CreateCatalogue();

            var creatingHeader = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            var hash = _testDataGenerator.CreateRandomByteArray(16);
            await catalogue.CompleteCreateAsync(creatingHeader.Id, 100, hash, CancellationToken.None);
            Assert.ThrowsAsync<FaasInvalidOperationException>(async () => { await catalogue.CancelWritingAsync(creatingHeader.Id, CancellationToken.None); });
        }
    }
}