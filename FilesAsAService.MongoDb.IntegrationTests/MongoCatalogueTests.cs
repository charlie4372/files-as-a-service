using FilesAsAService.UnitTests;
using MongoDB.Driver;
using NUnit.Framework;

namespace FilesAsAService.MongoDb.IntegrationTests
{
    [TestFixture(Category = "Integration")]
    public class MongoCatalogueTests : StandardCatalogueTests
    {
        private MongoClient _client;
        
        [SetUp]
        public void Setup()
        {
            _client = new MongoClient(@"mongodb://localhost:27017");
        }

        protected override IFaasCatalogue CreateCatalogue()
        {
            return new FaasMongoCatalogue("test-mongo-catalogue", _client, "faas-test");
        }
    }
}