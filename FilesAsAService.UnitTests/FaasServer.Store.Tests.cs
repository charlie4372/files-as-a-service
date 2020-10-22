using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;

namespace FilesAsAService.UnitTests
{
    [TestFixture(Category = "Unit")]
    public class FaasServerStoreTests
    {
        [Test]
        public void DoesContainsStore_ReturnTrue()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<IFaasFileStore>().Object);
            
            Assert.IsTrue(server.ContainsStore("test"));
        }
        
        [Test]
        public void DoesContainsStore_ReturnFalse_WhenStoreDoesNotExist()
        {
            var server = new FaasServer();
            
            Assert.IsFalse(server.ContainsStore("test"));
        }
        
        [Test]
        public void DoesAddStore_AddStore()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<IFaasFileStore>().Object);
            
            Assert.IsTrue(server.ContainsStore("test"));
        }
        
        [Test]
        public void DoesAddStore_Throw_WhenStoreAlreadyAdded()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<IFaasFileStore>().Object);

            Assert.Throws<ArgumentException>(() => server.Add("test", new Mock<IFaasFileStore>().Object));
        }
        
        [Test]
        public void DoesGetStore_ReturnStore()
        {
            var server = new FaasServer();
            var store = new Mock<IFaasFileStore>().Object;
            server.Add("test", store);
            
            Assert.AreEqual(store, server.GetStore("test"));
        }
        
        [Test]
        public void DoesGetStore_Throw_WhenStoredDoesntExist()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<IFaasFileStore>().Object);

            Assert.Throws<KeyNotFoundException>(() => server.GetContainer("test"));
        }
    }
}