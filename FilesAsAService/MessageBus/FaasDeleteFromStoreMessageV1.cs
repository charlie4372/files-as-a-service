using System;

namespace FilesAsAService.MessageBus
{
    /// <summary>
    /// Message for deleting items from the store.
    /// </summary>
    public class FaasDeleteFromStoreMessageV1 : FaasMessage
    {
        /// <summary>
        /// The stores name.
        /// </summary>
        public string StoreName { get; set; } = "";
        
        /// <summary>
        /// The items id.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public FaasDeleteFromStoreMessageV1()
        {
            Version = 1;
            Name = "delete-from-store";
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="storeName">The stores name.</param>
        /// <param name="id">The items id.</param>
        public FaasDeleteFromStoreMessageV1(string storeName, Guid id) : this()
        {
            DateCreatedUtc = DateTime.UtcNow;
            StoreName = storeName;
            Id = id;
        }
    }
}