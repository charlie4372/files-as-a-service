using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.InMemory
{
    /// <summary>
    /// Catalogue that is stored in memory.
    /// </summary>
    public class InMemoryFaasCatalogue : IFaasCatalogue
    {
        /// <summary>
        /// Protect access to the internal storage.
        /// </summary>
        private readonly Semaphore _lock = new Semaphore(1, 1);

        /// <summary>
        /// The size of the pages for listing.
        /// </summary>
        private const int PageSize = 100;

        /// <summary>
        /// When data is deleted, the record is set to null.
        /// This sames recalculating the whole index.
        /// TODO think about cleaning this up vs back filling the gaps.
        /// </summary>
        private readonly List<FaasFileHeader?> _data = new List<FaasFileHeader?>();
        private readonly Dictionary<Guid, int> _idIndex = new Dictionary<Guid, int>();
        
        /// <inheritdoc cref="GetAsync"/>
        public ValueTask<FaasFileHeader?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                return GetNoLock(id, cancellationToken);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Gets a header without using a lock.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The header, or null.</returns>
        private ValueTask<FaasFileHeader?> GetNoLock(Guid id, CancellationToken cancellationToken)
        {
            // Return null of the id isn't found.
            if (!_idIndex.ContainsKey(id))
                return new ValueTask<FaasFileHeader?>((FaasFileHeader?) null);

            // Return the header.
            var header = _data[_idIndex[id]];
            return new ValueTask<FaasFileHeader?>(new FaasFileHeader(header));
        }

        /// <inheritdoc cref="ListAsync"/>
        public ValueTask<IEnumerable<FaasFileHeader>> ListAsync(int pageNumber, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                return new ValueTask<IEnumerable<FaasFileHeader>>(_data.Skip(pageNumber * PageSize).Take(PageSize).Select(row => new FaasFileHeader(row)));
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc cref="StartCreateAsync"/>
        public ValueTask<FaasFileHeader> StartCreateAsync(string name, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                // Create the header.
                var fileHeader = new FaasFileHeader
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Length = 0,
                    DateCreatedUtc = DateTime.UtcNow,
                    DateUpdatedUtc = DateTime.UtcNow,
                    Status = FaasFileHeaderStatus.Creating,
                    Version = 1
                };

                // Add it and index it.
                _data.Add(fileHeader);
                _idIndex.Add(fileHeader.Id, _data.Count - 1);
                
                // Return it.
                return new ValueTask<FaasFileHeader>(fileHeader);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc cref="CompleteCreateAsync"/>
        public async ValueTask CompleteCreateAsync(Guid id, long length, byte[] hash, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                // Get the header. If its not found, throw.
                var header = await GetNoLock(id, cancellationToken);
                if (header == null)
                    throw new FaasFileNotFoundException();

                // Check that the header can be completed.
                if (header.Status != FaasFileHeaderStatus.Creating)
                    throw new FaasInvalidOperationException();

                // Update the fields.
                header.Length = length;
                header.Status = FaasFileHeaderStatus.Active;

                // Update stored copy.
                _data[_idIndex[id]] = header;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc cref="CancelCreateAsync"/>
        public async Task CancelCreateAsync(Guid id, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                // Get the header. If its not found, throw.
                var header = await GetNoLock(id, cancellationToken);
                if (header == null)
                    throw new FaasFileNotFoundException();

                // Check that the header can be completed.
                if (header.Status != FaasFileHeaderStatus.Creating)
                    throw new FaasInvalidOperationException();

                // Clear the header. 
                // Not deleting it since it will mess with indexes.
                _data[_idIndex[id]] = null;
                
                // Delete the index.
                _idIndex.Remove(id);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}