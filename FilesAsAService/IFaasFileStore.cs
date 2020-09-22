using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    /// <summary>
    /// The store for the file contents.
    /// </summary>
    public interface IFaasFileStore : IDisposable
    {
        /// <summary>
        /// Creates a file in the store.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="stream">The contents of the file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentException">Invalid arguments.</exception>
        /// <exception cref="FaasFileExistsException">The file exists.</exception>
        /// <returns></returns>
        Task CreateAsync(Guid id, Stream stream, CancellationToken cancellationToken);
        
        /// <summary>
        /// Reads a file from the store.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentException">Invalid arguments.</exception>
        /// <exception cref="FaasFileNotFoundException">The file doesn't exist.</exception>
        /// <returns>The file contents.</returns>
        Task<Stream> ReadAsync(Guid id, CancellationToken cancellationToken);
    }
}