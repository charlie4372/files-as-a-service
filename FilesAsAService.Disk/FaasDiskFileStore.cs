using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.Disk
{
    public class FaasDiskFileStore : IFaasFileStore
    {
        private readonly string _baseFolder;

        public FaasDiskFileStore(string baseFolder)
        {
            _baseFolder = baseFolder;
        }

        private string GetPathToId(Guid id)
        {
            return Path.Combine(_baseFolder, id.ToString());
        }
        
        public async Task CreateAsync(Guid id, Stream stream, CancellationToken cancellationToken)
        {
            var fullPath = GetPathToId(id);
            if (File.Exists(fullPath))
                throw new FaasFileExistsException();
            
            using (var output = File.Create(fullPath))
            {
                await stream.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }
        }

        public Task<Stream> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            var fullPath = GetPathToId(id);
            if (!File.Exists(fullPath))
                throw new FaasFileNotFoundException();
            
            return Task.FromResult<Stream>(File.OpenRead(Path.Combine(_baseFolder, id.ToString())));
        }
        
        public void Dispose()
        {
        }
    }
}