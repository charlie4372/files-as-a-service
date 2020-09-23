using FilesAsAService.InMemory;
using NUnit.Framework;

namespace FilesAsAService.UnitTests.InMemory
{
    [TestFixture]
    public class InMemoryFileStoreTests : StandardFileStoreTests
    {
        protected override IFaasFileStore CreateFileStore()
        {
            return new InMemoryFaasFileStore();
        }
    }
}