using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    public interface IFaasFileStore
    {
        Task CreateAsync(Guid fileId, Stream stream, CancellationToken cancellationToken);
        
        Task<Stream> ReadAsync(Guid fileId, CancellationToken cancellationToken);

        // Task ReplaceAsync(Guid fileId, Stream stream, CancellationToken cancellationToken);
    }
}