using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    public interface IFaasContainer
    {
        /// <summary>
        /// Creates a file.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="stream">The contents</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The id.</returns>
        Task<Guid> CreateAsync(string name, Stream stream, CancellationToken cancellationToken);

        /// <summary>
        /// Reads a file.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="id">The id.</param>
        /// <exception cref="FaasFileNotFoundException">The file was not found.</exception>
        /// /// <exception cref="FaasFileVersionNotFoundException">The file version was not found.</exception>
        /// <returns>The contents.</returns>
        Task<Stream> ReadAsync(Guid id, Guid? versionId, CancellationToken cancellationToken);
    }
}