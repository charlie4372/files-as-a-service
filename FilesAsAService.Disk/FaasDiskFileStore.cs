using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.Disk
{
    /// <summary>
    /// A file store whose contents are stored on the file system.
    /// </summary>
    public class FaasDiskFileStore : IFaasFileStore
    {
        /// <summary>
        /// The base folder of the files.
        /// </summary>
        private readonly string _baseFolder;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="baseFolder">The base folder of the files.</param>
        public FaasDiskFileStore(string baseFolder)
        {
            _baseFolder = baseFolder;
        }

        /// <summary>
        /// Gets the path to an id.
        /// </summary>
        /// <param name="id">The file id.</param>
        /// <returns>The absolute path to the file.</returns>
        private string GetPathToId(Guid id)
        {
            return Path.Combine(_baseFolder, id.ToString());
        }

        /// <inheritdoc cref="ContainsAsync"/>
        public Task<bool> ContainsAsync(Guid id, CancellationToken cancellationToken)
        {
            var fullPath = GetPathToId(id);
            return Task.FromResult(File.Exists(fullPath));
        }

        /// <inheritdoc cref="CreateAsync"/>
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

        /// <inheritdoc cref="ReadAsync"/>
        public Task<Stream> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            var fullPath = GetPathToId(id);
            if (!File.Exists(fullPath))
                throw new FaasFileNotFoundException();
            
            return Task.FromResult<Stream>(File.OpenRead(Path.Combine(_baseFolder, id.ToString())));
        }
        
        /// <inheritdoc cref="DeleteAsync"/>
        public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var fullPath = GetPathToId(id);
            if (!File.Exists(fullPath))
                throw new FaasFileNotFoundException();
            
            File.Delete(fullPath);
            
            return Task.CompletedTask;
        }
        
        /// <inheritdoc cref="Dispose"/>
        public void Dispose()
        {
        }
    }
}