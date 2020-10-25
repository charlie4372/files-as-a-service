using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.MessageBus
{
    /// <summary>
    /// Processes messages sent via the message bus.
    /// </summary>
    public class FaasMessageProcessor
    {
        /// <summary>
        /// The containers.
        /// </summary>
        private readonly IList<FaasContainer> _containers = new List<FaasContainer>();
        
        /// <summary>
        /// SyncRoot for concurrency control.
        /// </summary>
        public object SyncRoot { get; } = new object();

        /// <summary>
        /// Adds a container to the collection.
        /// </summary>
        /// <param name="container">The container.</param>
        public void AddContainer(FaasContainer container)
        {
            lock (SyncRoot)
            {
                if (_containers.Contains(container)) throw new ArgumentException("Item already added.", nameof(container));
                
                _containers.Add(container);
            }
        }

        /// <summary>
        /// All of the stores from all of the containers.
        /// </summary>
        private IEnumerable<IFaasFileStore> Stores
        {
            get
            {
                lock (SyncRoot)
                {
                    return _containers.SelectMany(container => container.Stores);
                }
            }
        }
        
        /// <summary>
        /// Processes a delete from store message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async Task Process(FaasDeleteFromStoreMessageV1 message, CancellationToken cancellationToken)
        {
            // TODO Add error handling for invalid store.
            var store = Stores.FirstOrDefault(row => row.Name == message.StoreName);
            if (store == null)
                // TODO better error.
                throw new Exception("Invalid store.");

            var fileExists = await store.ContainsAsync(message.Id, cancellationToken);
            if (!fileExists)
                return;
            
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            await store.DeleteAsync(message.Id, cancellationToken);
        }
    }
}