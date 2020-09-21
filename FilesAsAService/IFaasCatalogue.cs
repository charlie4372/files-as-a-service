using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    public interface IFaasCatalogue
    {
        // public Task<FaasFileHeader> GetAsync(Guid fileId);

        public ValueTask<FaasFileHeader?> GetAsync(Guid id, CancellationToken cancellationToken);
        
        public ValueTask<IEnumerable<FaasFileHeader>> ListAsync(int pageNumber, CancellationToken cancellationToken);

        public ValueTask<FaasFileHeader> StartCreateAsync(string name, CancellationToken cancellationToken);
        
        public ValueTask CompleteCreateAsync(Guid id, long length, byte[] hash, CancellationToken cancellationToken);
        
        public Task CancelCreateAsync(Guid id, CancellationToken cancellationToken);

        // public Task UpdateAsync(FaasFileHeader fileHeader);
        //
        // public Task RemoveAsync(Guid fileId);
    }
}