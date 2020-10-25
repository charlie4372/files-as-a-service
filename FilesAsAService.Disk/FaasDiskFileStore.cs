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
        /// The number of buckets to use.
        /// </summary>
        private readonly int _numberOfBuckets;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="name">The name. The message bus will use this to locate it.</param>
        /// <param name="baseFolder">The base folder of the files.</param>
        /// <param name="numberOfBuckets">The number of buckets to use. Use 0 to disable buckets.</param>
        public FaasDiskFileStore(string name, string baseFolder, int numberOfBuckets = 0)
        {
            if (numberOfBuckets < 0) throw new ArgumentOutOfRangeException(nameof(numberOfBuckets));

            _baseFolder = baseFolder;
            _numberOfBuckets = numberOfBuckets;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets the path to an id.
        /// </summary>
        /// <param name="id">The file id.</param>
        /// <returns>The absolute path to the file.</returns>
        private string GetPathToId(Guid id)
        {
            if (_numberOfBuckets == 0)
                return Path.Combine(_baseFolder, id.ToString());

            var bucket = id.GetHashCode() % _numberOfBuckets;
            return Path.Combine(_baseFolder, bucket.ToString("X"), id.ToString());
        }

        /// <inheritdoc cref="Name"/>summary>
        public string Name { get; }

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

            await using var output = File.Create(fullPath);
            await stream.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
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
            GC.SuppressFinalize(this);
        }
    }
}