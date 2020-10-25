using FilesAsAService.InMemory;
using NUnit.Framework;

namespace FilesAsAService.UnitTests.InMemory
{
    [TestFixture(Category = "Unit")]
    public class InMemoryFaasCatalogueTests : StandardCatalogueTests
    {
        protected override IFaasCatalogue CreateCatalogue()
        {
            return new InMemoryFaasCatalogue("test-inmemory-catalogue");
        }
    }
}