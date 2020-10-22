using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.MessageBus;
using Moq;
using NUnit.Framework;

namespace FilesAsAService.UnitTests
{
    [TestFixture(Category = "Unit")]
    public class FaasContainerTests
    {
        private readonly TestDataGenerator _testDataGenerator = new TestDataGenerator();
        
        private Mock<IFaasCatalogue> _catalogueMock;
        private Mock<IFaasFileStore> _storeMock;
        private Mock<IFaasMessageBus> _messageBusMock;

        [SetUp]
        public void SetUp()
        {
            _catalogueMock = new Mock<IFaasCatalogue>();
            _storeMock = new Mock<IFaasFileStore>();
            _messageBusMock = new Mock<IFaasMessageBus>();
        }

        private FaasContainer CreateContainer()
        {
            return new FaasContainer(_catalogueMock.Object, _storeMock.Object, "test", _messageBusMock.Object);
        }

        [Test]
        public async Task DoesCreateAsyncCreateFile()
        {
            var fileVersionId = new FaasFileVersionId(Guid.NewGuid(), Guid.NewGuid());
            var data = _testDataGenerator.CreateRandomByteArray(100);
            await using var dataStream = new MemoryStream(data);

            _catalogueMock.Setup(m => m.StartCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => fileVersionId);

            _storeMock.Setup(m => m.CreateAsync(It.Is<Guid>(v => v == fileVersionId.FileId), It.IsAny<Stream>(), It.IsAny<CancellationToken>()));

            _catalogueMock.Setup(m => m.CompleteWritingAsync(
                It.Is<Guid>(v => v == fileVersionId.FileId),
                It.Is<Guid>(v => v == fileVersionId.VersionId),
                It.Is<long>(v => v == 100),
                It.Is<byte[]>(v => AreEqual(v, data)),
                It.IsAny<CancellationToken>()));

            var container = CreateContainer();
            var id = await container.CreateAsync("text.txt", dataStream, CancellationToken.None);

            Assert.AreEqual(fileVersionId.FileId, id);
            
            _catalogueMock.Verify(m => m.StartCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _storeMock.Verify(m => m.CreateAsync(It.Is<Guid>(v => v == fileVersionId.FileId), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
            _catalogueMock.Verify(m => m.CompleteWritingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            
            _catalogueMock.Verify(m => m.CancelWritingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
        
        [Test]
        public async Task DoesCreateAsyncCancelIsExceptionIsThrown()
        {
            var fileVersionId = new FaasFileVersionId(Guid.NewGuid(), Guid.NewGuid());
            var data = _testDataGenerator.CreateRandomByteArray(100);
            await using var dataStream = new MemoryStream(data);

            _catalogueMock.Setup(m => m.StartCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => fileVersionId);

            _storeMock.Setup(m => m.CreateAsync(It.Is<Guid>(v => v == fileVersionId.FileId), It.IsAny<Stream>(), It.IsAny<CancellationToken>()));

            _catalogueMock.Setup(m => m.CompleteWritingAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
                .Throws(new Exception());

            _catalogueMock.Setup(m => m.CancelWritingAsync(
                It.Is<Guid>(v => v == fileVersionId.FileId),
                It.Is<Guid>(v => v == fileVersionId.VersionId),
                It.IsAny<CancellationToken>()));

            var container = CreateContainer();
            var ex = Assert.ThrowsAsync<Exception>(async () => await container.CreateAsync("text.txt", dataStream, CancellationToken.None));

            Assert.IsNotNull(ex);
            
            _catalogueMock.Verify(m => m.StartCreateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _storeMock.Verify(m => m.CreateAsync(It.Is<Guid>(v => v == fileVersionId.FileId), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
            _catalogueMock.Verify(m => m.CompleteWritingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<long>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            
            _catalogueMock.Verify(m => m.CancelWritingAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Test]
        public async Task DoesReadAsyncRetrieveCurrentFile()
        {
            var fileVersionId = new FaasFileVersionId(Guid.NewGuid(), Guid.NewGuid());
            var header = new FaasFileHeader
            {
                FileId = fileVersionId.FileId,
                VersionId = fileVersionId.VersionId,
                Versions = new[]
                {
                    new FaasFileHeaderVersion {VersionId = fileVersionId.VersionId},
                }
            };
            var data = _testDataGenerator.CreateRandomByteArray(100);
            await using var dataStream = new MemoryStream(data);

            _catalogueMock.Setup(m => m.GetAsync(It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => header);

            _storeMock.Setup(m => m.ReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => dataStream);

            var container = CreateContainer();
            var stream = await container.ReadAsync(header.FileId, null, CancellationToken.None);

            Assert.AreEqual(dataStream, stream);

            _catalogueMock.Verify(m => m.GetAsync(It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>())
                , Times.Once);
            _storeMock.Verify(m => m.ReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task DoesReadAsyncRetrieveSpecificVersion()
        {
            var fileVersionId = new FaasFileVersionId(Guid.NewGuid(), Guid.NewGuid());
            var header = new FaasFileHeader
            {
                FileId = fileVersionId.FileId,
                VersionId = fileVersionId.VersionId,
                Versions = new[]
                {
                    new FaasFileHeaderVersion {VersionId = fileVersionId.VersionId},
                }
            };
            var data = _testDataGenerator.CreateRandomByteArray(100);
            await using var dataStream = new MemoryStream(data);

            _catalogueMock.Setup(m => m.GetAsync(It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => header);

            _storeMock.Setup(m => m.ReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => dataStream);

            var container = CreateContainer();
            var stream = await container.ReadAsync(header.FileId, header.VersionId, CancellationToken.None);

            Assert.AreEqual(dataStream, stream);

            _catalogueMock.Verify(m => m.GetAsync(It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>())
                , Times.Once);
            _storeMock.Verify(m => m.ReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Test]
        public async Task DoesReadAsyncThrowOnInvalidVersionId()
        {
            var fileVersionId = new FaasFileVersionId(Guid.NewGuid(), Guid.NewGuid());
            var header = new FaasFileHeader
            {
                FileId = fileVersionId.FileId,
                VersionId = fileVersionId.VersionId,
                Versions = new[]
                {
                    new FaasFileHeaderVersion {VersionId = fileVersionId.VersionId},
                }
            };
            var data = _testDataGenerator.CreateRandomByteArray(100);
            await using var dataStream = new MemoryStream(data);

            _catalogueMock.Setup(m => m.GetAsync(It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => header);

            _storeMock.Setup(m => m.ReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => dataStream);

            var container = CreateContainer();
            Assert.ThrowsAsync<FaasFileVersionNotFoundException>(async () => await container.ReadAsync(header.FileId, Guid.NewGuid(), CancellationToken.None));

            _catalogueMock.Verify(m => m.GetAsync(It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>())
                , Times.Once);
            _storeMock.Verify(m => m.ReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        
        [Test]
        public async Task DoesReadAsyncThrowOnInvalidId()
        {
            var fileVersionId = new FaasFileVersionId(Guid.NewGuid(), Guid.NewGuid());
            var header = new FaasFileHeader
            {
                FileId = fileVersionId.FileId,
                VersionId = fileVersionId.VersionId,
                Versions = new[]
                {
                    new FaasFileHeaderVersion {VersionId = fileVersionId.VersionId},
                }
            };
            var data = _testDataGenerator.CreateRandomByteArray(100);
            await using var dataStream = new MemoryStream(data);

            _catalogueMock.Setup(m => m.GetAsync(It.Is<Guid>(v => v == header.FileId),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => header);

            _storeMock.Setup(m => m.ReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => dataStream);

            var container = CreateContainer();
            Assert.ThrowsAsync<FaasFileNotFoundException>(async () => await container.ReadAsync(Guid.NewGuid(), null, CancellationToken.None));

            _catalogueMock.Verify(m => m.GetAsync(It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>())
                , Times.Once);
            _storeMock.Verify(m => m.ReadAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private bool AreEqual(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length)
                return false;
            
            for (var i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i])
                    return false;
            }

            return true;
        }
    }
}