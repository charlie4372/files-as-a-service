using System.Threading.Tasks;

namespace FilesAsAService.MessageBus
{
    /// <summary>
    /// The message bus.
    /// </summary>
    public interface IFaasMessageBus
    {
        /// <summary>
        /// Adds container to the message bus.
        /// This will bind the container to the message bus so that the container can post messages,
        /// and so that messages can be processed against it.
        /// </summary>
        /// <param name="container">The container.</param>
        void AddContainer(FaasContainer container);
        
        /// <summary>
        /// Sends a delete from store message.
        /// </summary>
        /// <param name="fromStoreMessage">The message.</param>
        /// <returns></returns>
        Task Send(FaasDeleteFromStoreMessageV1 fromStoreMessage);

        /// <summary>
        /// Raised when a message is processed.
        /// </summary>
        event FaasMessageEventHandler MessageProcessed;
    }
}