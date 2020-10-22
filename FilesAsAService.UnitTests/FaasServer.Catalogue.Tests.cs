using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;

namespace FilesAsAService.UnitTests
{
    [TestFixture(Category = "Unit")]
    public class FaasServerCatalogueTests
    {
        [Test]
        public void DoesContainsCatalogue_ReturnTrue()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<IFaasCatalogue>().Object);
            
            Assert.IsTrue(server.ContainsCatalogue("test"));
        }
        
        [Test]
        public void DoesContainsCatalogue_ReturnFalse_WhenCatalogueDoesNotExist()
        {
            var server = new FaasServer();
            
            Assert.IsFalse(server.ContainsCatalogue("test"));
        }
        
        [Test]
        public void DoesAddCatalogue_AddCatalogue()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<IFaasCatalogue>().Object);
            
            Assert.IsTrue(server.ContainsCatalogue("test"));
        }
        
        [Test]
        public void DoesAddCatalogue_Throw_WhenCatalogueAlreadyAdded()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<IFaasCatalogue>().Object);

            Assert.Throws<ArgumentException>(() => server.Add("test", new Mock<IFaasCatalogue>().Object));
        }
        
        [Test]
        public void DoesGetCatalogue_ReturnCatalogue()
        {
            var server = new FaasServer();
            var catalogue = new Mock<IFaasCatalogue>().Object;
            server.Add("test", catalogue);
            
            Assert.AreEqual(catalogue, server.GetCatalogue("test"));
        }
        
        [Test]
        public void DoesGetCatalogue_Throw_WhenCataloguedDoesntExist()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<IFaasCatalogue>().Object);

            Assert.Throws<KeyNotFoundException>(() => server.GetContainer("test"));
        }
    }
}