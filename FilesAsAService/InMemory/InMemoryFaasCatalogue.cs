using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.InMemory
{
    public class InMemoryFaasCatalogue : IFaasCatalogue
    {
        private readonly Semaphore _lock = new Semaphore(1, 1);

        private const int PageSize = 100;

        /// <summary>
        /// When data is deleted, the record is set to null.
        /// This sames recalculating the whole index.
        /// TODO think about cleaning this up vs back filling the gaps.
        /// </summary>
        private readonly List<FaasFileHeader?> _data = new List<FaasFileHeader?>();
        private readonly Dictionary<Guid, int> _idIndex = new Dictionary<Guid, int>();
        
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

        private ValueTask<FaasFileHeader?> GetNoLock(Guid id, CancellationToken cancellationToken)
        {
            if (!_idIndex.ContainsKey(id))
                return new ValueTask<FaasFileHeader?>((FaasFileHeader?) null);

            var header = _data[_idIndex[id]];
            if (header == null)
                return new ValueTask<FaasFileHeader?>((FaasFileHeader?) null);

            return new ValueTask<FaasFileHeader?>(new FaasFileHeader(header));
        }

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

        public ValueTask<FaasFileHeader> StartCreateAsync(string name, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                var fileHeader = new FaasFileHeader
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Length = 0,
                    DateCreatedUtc = DateTime.UtcNow,
                    DateUpdatedUtc = DateTime.UtcNow,
                    Status = FaasFileHeaderStatus.Creating
                };

                _data.Add(fileHeader);
                _idIndex.Add(fileHeader.Id, _data.Count - 1);
                return new ValueTask<FaasFileHeader>(fileHeader);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async ValueTask CompleteCreateAsync(Guid id, long length, byte[] hash, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                var header = await GetNoLock(id, cancellationToken);
                if (header == null)
                    throw new FaasFileNotFoundException();

                header.Length = length;
                header.Status = FaasFileHeaderStatus.Active;

                _data[_idIndex[id]] = header;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task CancelCreateAsync(Guid id, CancellationToken cancellationToken)
        {
            _lock.WaitOne();
            try
            {
                var header = await GetNoLock(id, cancellationToken);
                if (header == null)
                    throw new FaasFileNotFoundException();

                _data[_idIndex[id]] = null;
                _idIndex.Remove(id);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}