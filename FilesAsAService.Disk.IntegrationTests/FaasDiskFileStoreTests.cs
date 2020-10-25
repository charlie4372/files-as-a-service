using System;
using System.IO;
using FilesAsAService.UnitTests;
using NUnit.Framework;

namespace FilesAsAService.Disk.IntegrationTests
{
    [TestFixture(Category = "Integration")]
    public class FaasDiskFileStoreTests : StandardFileStoreTests
    {
        private string _testFolder;
        
        [SetUp]
        public void Setup()
        {
            _testFolder = Path.Combine(Environment.CurrentDirectory, "FaasDiskFileStoreTests");
            Directory.CreateDirectory(_testFolder);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var file in Directory.GetFiles(_testFolder, "*.*"))
            {
                File.Delete(file);
            }
            
            Directory.Delete(_testFolder);
        }

        protected override IFaasFileStore CreateFileStore()
        {
            return new FaasDiskFileStore("test-disk-store", _testFolder);
        }
    }
}