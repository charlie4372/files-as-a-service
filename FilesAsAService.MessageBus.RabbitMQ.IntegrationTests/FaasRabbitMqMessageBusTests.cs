using System.Linq;
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
        private FaasContainer _container;
        private FaasRabbitMqMessageBus _messageBus;
        
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
            
            _container = new FaasContainer();
            _container.AddCatalogue(new InMemoryFaasCatalogue("test-inmemory-catalogue"));
            _container.AddStore(new InMemoryFaasFileStore("test-inmemory-store"));
            _messageBus = new FaasRabbitMqMessageBus(options);
            _messageBus.AddContainer(_container);
        }

        [Test]
        public async Task Test1()
        {
            await using var testData = _testDataGenerator.CreateRandomStream(100);
            var createdFileId = await _container.CreateAsync("test.txt", testData, CancellationToken.None);
            var fileHeader = await _container.GetHeaderAsync(createdFileId, CancellationToken.None);
            Assert.NotNull(fileHeader);
            Assert.NotNull(fileHeader.VersionId);

            var store = _container.Stores.First();
            
            var fileExists = await store.ContainsAsync(fileHeader.VersionId.Value, CancellationToken.None);
            Assert.IsTrue(fileExists);;
            
            var taskCompletion = new TaskCompletionSource<IFaasMessage>();
            _messageBus.MessageProcessed += (sender, args) =>
            {
                if (args.Exception != null)
                    taskCompletion.SetException(args.Exception);
                else
                    taskCompletion.SetResult(args.Message);
            };

            var message = new FaasDeleteFromStoreMessageV1(_container.Stores.First().Name, fileHeader.VersionId.Value);
            await _messageBus.Send(message);

            await taskCompletion.Task;
            
            var currentFileExists = await store.ContainsAsync(fileHeader.VersionId.Value, CancellationToken.None);
            Assert.IsFalse(currentFileExists);
        }
        
        [TearDown]
        public void TearDown()
        {
            _messageBus?.Dispose();
            _container?.Dispose();
        }
    }
}