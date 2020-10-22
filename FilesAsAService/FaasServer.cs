using System;
using System.Collections.Generic;

namespace FilesAsAService
{
    /// <summary>
    /// The server.
    /// This is the root of everything.
    /// It also acts as a locator to find things by their name.
    /// </summary>
    public class FaasServer
    {
        /// <summary>
        /// The stores.
        /// </summary>
        private readonly IDictionary<string, IFaasFileStore> _stores = new Dictionary<string, IFaasFileStore>(StringComparer.InvariantCultureIgnoreCase);
        
        /// <summary>
        /// The catalogues.
        /// </summary>
        private readonly IDictionary<string, IFaasCatalogue> _catalogues = new Dictionary<string, IFaasCatalogue>(StringComparer.InvariantCultureIgnoreCase);
        
        /// <summary>
        /// The containers.
        /// </summary>
        private readonly IDictionary<string, FaasContainer> _containers = new Dictionary<string, FaasContainer>(StringComparer.InvariantCultureIgnoreCase);
        
        /// <summary>
        /// Sync root for concurrency control.
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        /// Determines if a store exists.
        /// </summary>
        /// <param name="name">The store name</param>
        /// <returns>True if found.</returns>
        public bool ContainsStore(string name)
        {
            lock (_syncRoot)
            {
                return _stores.ContainsKey(name);
            }
        }

        /// <summary>
        /// Gets a store.
        /// Throws is the store cannot be found.
        /// </summary>
        /// <param name="name">the store name.</param>
        /// <returns>The store.</returns>
        public IFaasFileStore GetStore(string name)
        {
            lock (_syncRoot)
            {
                return _stores[name];
            }
        }

        /// <summary>
        /// Adds a store.
        /// Throws if the store already exists.
        /// </summary>
        /// <param name="name">The store name.</param>
        /// <param name="store">The store.</param>
        public void Add(string name, IFaasFileStore store)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (store == null) throw new ArgumentNullException(nameof(name));

            lock (_syncRoot)
            {
                _stores.Add(name, store);
            }
        }
        
        /// <summary>
        /// Determines if a catalogue exists.
        /// </summary>
        /// <param name="name">The store name</param>
        /// <returns>True if found.</returns>
        public bool ContainsCatalogue(string name)
        {
            lock (_syncRoot)
            {
                return _stores.ContainsKey(name);
            }
        }

        /// <summary>
        /// Gets a store.
        /// Throws is the catalogue cannot be found.
        /// </summary>
        /// <param name="name">The catalogue name.</param>
        /// <returns>The catalogue.</returns>
        public IFaasCatalogue GetCatalogue(string name)
        {
            lock (_syncRoot)
            {
                return _catalogues[name];
            }
        }

        /// <summary>
        /// Adds a catalogue.
        /// Throws if the catalogue already exists.
        /// </summary>
        /// <param name="name">The catalogue name.</param>
        /// <param name="catalogue">The catalogue.</param>
        public void Add(string name, IFaasCatalogue catalogue)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (catalogue == null) throw new ArgumentNullException(nameof(name));

            lock (_syncRoot)
            {
                _catalogues.Add(name, catalogue);
            }
        }
        
        
        /// <summary>
        /// Determines if a container exists.
        /// </summary>
        /// <param name="name">The container name</param>
        /// <returns>True if found.</returns>
        public bool ContainsContainer(string name)
        {
            lock (_syncRoot)
            {
                return _containers.ContainsKey(name);
            }
        }

        /// <summary>
        /// Gets a container.
        /// Throws is the catalogue cannot be found.
        /// </summary>
        /// <param name="name">The container name.</param>
        /// <returns>The container.</returns>
        public FaasContainer GetContainer(string name)
        {
            lock (_syncRoot)
            {
                return _containers[name];
            }
        }

        /// <summary>
        /// Adds a container.
        /// Throws if the container already exists.
        /// </summary>
        /// <param name="name">The container name.</param>
        /// <param name="container">The container.</param>
        public void Add(string name, FaasContainer container)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
            if (container == null) throw new ArgumentNullException(nameof(name));

            lock (_syncRoot)
            {
                _containers.Add(name, container);
            }
        }
    }
}