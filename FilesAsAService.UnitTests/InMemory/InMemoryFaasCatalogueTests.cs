using System;
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

            var header = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);
            Assert.IsNotNull(header);
            Assert.AreNotEqual(Guid.Empty, header.Id);
            Assert.AreEqual("test.txt", header.Name);
            Assert.AreEqual(0, header.Length);
            Assert.IsTrue(header.DateCreatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsTrue(header.DateUpdatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsNull(header.DateDeletedUtc);
            Assert.AreEqual(FaasFileHeaderStatus.Creating, header.Status);
        }
        
        [Test]
        public async Task DoesStartGetAsyncRetrieveCreatingHeader()
        {
            var catalogue = CreateCatalogue();

            var creatingHeader = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            var header = await catalogue.GetAsync(creatingHeader.Id, CancellationToken.None);
                            
            Assert.IsNotNull(header);
            Assert.AreNotEqual(Guid.Empty, header.Id);
            Assert.AreEqual("test.txt", header.Name);
            Assert.AreEqual(0, header.Length);
            Assert.IsTrue(header.DateCreatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsTrue(header.DateUpdatedUtc.Subtract(DateTime.UtcNow).TotalSeconds < 1);
            Assert.IsNull(header.DateDeletedUtc);
            Assert.AreEqual(FaasFileHeaderStatus.Creating, header.Status);
        }
        
        [Test]
        public async Task DoesStartGetAsyncReturnNullWhenNotFound()
        {
            var catalogue = CreateCatalogue();

            var header = await catalogue.GetAsync(Guid.NewGuid(), CancellationToken.None);
                            
            Assert.IsNull(header);
        }
        
        [Test]
        public async Task DoesCompleteCreateAsyncUpdateFields()
        {
            var catalogue = CreateCatalogue();

            var creatingHeader = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            var hash = _testDataGenerator.CreateRandomByteArray(16);
            await catalogue.CompleteCreateAsync(creatingHeader.Id, 100, hash, CancellationToken.None);
            var header = await catalogue.GetAsync(creatingHeader.Id, CancellationToken.None);
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
        public async Task DoesCancelCreateAsyncDeleteHeader()
        {
            var catalogue = CreateCatalogue();

            var creatingHeader = await catalogue.StartCreateAsync("test.txt", CancellationToken.None);

            await catalogue.CancelCreateAsync(creatingHeader.Id, CancellationToken.None);
            var header = await catalogue.GetAsync(creatingHeader.Id, CancellationToken.None);
            Assert.IsNull(header);
        }
    }
}