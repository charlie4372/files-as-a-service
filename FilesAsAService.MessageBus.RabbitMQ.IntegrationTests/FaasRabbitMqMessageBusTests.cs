using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FilesAsAService.InMemory;
using FilesAsAService.UnitTests;
using NUnit.Framework;

namespace FilesAsAService.MessageBus.RabbitMQ.IntegrationTests
{
    [TestFixture(Category = "Integration")]
    public class FaasRabbitMqMessageBusTests
    {
        private TestDataGenerator _testDataGenerator;
        private InMemoryFaasCatalogue _catalogue;
        private InMemoryFaasFileStore _fileStore;
        private FaasContainer _container;
        private FaasRabbitMqMessageBus _messageBus;
        private FaasMessageProcessor _messageProcessor;
        
        [SetUp]
        public void Setup()
        {
            var options = new FaasRabbitMqOptions
            {
                UserName = "faas-test",
                Password = "faas-test",
                HostName = "localhost",
                VirtualHost = "faas-test"
            };
            
            _testDataGenerator = new TestDataGenerator();
            _fileStore = new InMemoryFaasFileStore();
            _catalogue = new InMemoryFaasCatalogue();
            var containers = new List<FaasContainer>();
            _messageProcessor = new FaasMessageProcessor(containers);
            _messageBus = new FaasRabbitMqMessageBus(options, _messageProcessor);
            _container = new FaasContainer(_catalogue, _fileStore, "test", _messageBus);
            containers.Add(_container);
        }

        [Test]
        public async Task Test1()
        {
            await using var testData = _testDataGenerator.CreateRandomStream(100);
            var createdFileId = await _container.CreateAsync("test.txt", testData, CancellationToken.None);
            var fileHeader = await _container.GetHeaderAsync(createdFileId, CancellationToken.None);
            Assert.NotNull(fileHeader);
            Assert.NotNull(fileHeader.VersionId);
            
            var fileExists = await _fileStore.ContainsAsync(fileHeader.VersionId.Value, CancellationToken.None);
            Assert.IsTrue(fileExists);;
            
            var taskCompletion = new TaskCompletionSource<IFaasMessage>();
            _messageBus.MessageProcessed += (sender, args) =>
            {
                if (args.Exception != null)
                    taskCompletion.SetException(args.Exception);
                else
                    taskCompletion.SetResult(args.Message);
            };

            var message = new FaasDeleteFileVersionMessageV1(_container.Name, fileHeader.FileId, fileHeader.VersionId.Value);
            await _messageBus.Send(message);

            await taskCompletion.Task;
            
           // Assert.AreEqual(message, taskCompletion.Task.Result);
            var currentHeader = await _container.GetHeaderAsync(createdFileId, CancellationToken.None);
            Assert.NotNull(currentHeader);
            Assert.AreEqual(0, currentHeader.Versions.Length);
            var currentFileExists = await _fileStore.ContainsAsync(fileHeader.VersionId.Value, CancellationToken.None);
            Assert.IsFalse(currentFileExists);
        }
        
        [TearDown]
        public void TearDown()
        {
            _messageBus?.Dispose();
            _fileStore?.Dispose();
        }
    }
}