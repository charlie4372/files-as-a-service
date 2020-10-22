using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;

namespace FilesAsAService.UnitTests
{
    [TestFixture(Category = "Unit")]
    public class FaasServerContainerTests
    {
        [Test]
        public void DoesContainsContainer_ReturnTrue()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<FaasContainer>().Object);
            
            Assert.IsTrue(server.ContainsContainer("test"));
        }
        
        [Test]
        public void DoesContainsContainer_ReturnFalse_WhenContainerDoesNotExist()
        {
            var server = new FaasServer();
            
            Assert.IsFalse(server.ContainsContainer("test"));
        }
        
        [Test]
        public void DoesAddContainer_AddContainer()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<FaasContainer>().Object);
            
            Assert.IsTrue(server.ContainsContainer("test"));
        }
        
        [Test]
        public void DoesAddContainer_Throw_WhenContainerAlreadyAdded()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<FaasContainer>().Object);

            Assert.Throws<ArgumentException>(() => server.Add("test", new Mock<FaasContainer>().Object));
        }
        
        [Test]
        public void DoesGetContainer_ReturnContainer()
        {
            var server = new FaasServer();
            var container = new Mock<FaasContainer>().Object;
            server.Add("test", container);
            
            Assert.AreEqual(container, server.GetContainer("test"));
        }
        
        [Test]
        public void DoesGetContainer_Throw_WhenContainerDoesntExist()
        {
            var server = new FaasServer();
            server.Add("test", new Mock<FaasContainer>().Object);

            Assert.Throws<KeyNotFoundException>(() => server.GetContainer("test"));
        }
    }
}