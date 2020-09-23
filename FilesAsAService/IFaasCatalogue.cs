using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService
{
    /// <summary>
    /// The catalogue of files.
    /// </summary>
    public interface IFaasCatalogue
    {
        /// <summary>
        /// Gets a header from the catalogue.
        /// </summary>
        /// <param name="fileId">The file id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="FaasFileNotFoundException">If the id is not found.</exception>
        /// <exception cref="ArgumentException">If the id is invalid.</exception>
        /// <returns>The header or null.</returns>
        public ValueTask<FaasFileHeader?> GetAsync(Guid fileId, CancellationToken cancellationToken);
        
        /// <summary>
        /// Lists all of the files in the catalogue.
        /// </summary>
        /// <param name="pageNumber">the page number to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentException">Invalid arguments.</exception>
        /// <returns>The headers found.</returns>
        public ValueTask<IEnumerable<FaasFileHeader>> ListAsync(int pageNumber, CancellationToken cancellationToken);

        /// <summary>
        /// Starts creating a header.
        /// After the file is added to the store, <see cref="CompleteCreateAsync"/> completes the creation and makes the header available.
        /// Call <see cref="CancelWritingAsync"/> to cancel the creation.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The file id.</returns>
        public ValueTask<FaasFileVersionId> StartCreateAsync(string name, CancellationToken cancellationToken);

        /// <summary>
        /// Starts write a new version.
        /// After the file is written to the store, <see cref="CompleteCreateAsync"/> completes the writing and makes the header available.
        /// Call <see cref="CancelWritingAsync"/> to cancel the writing.
        /// </summary>
        /// <param name="fileId">The fileId.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The version id..</returns>
        public ValueTask<FaasFileVersionId> StartWritingAsync(Guid fileId, CancellationToken cancellationToken);
        
        /// <summary>
        /// Completes the writing process.
        /// </summary>
        /// <param name="fileId">The file id.</param>
        /// /// <param name="versionId">The versionId id.</param>
        /// <param name="length">The length of the file.</param>
        /// <param name="hash">The hash of the file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentException">Invalid parameters.</exception>
        /// <exception cref="FaasFileNotFoundException">Invalid id.</exception>
        /// <exception cref="FaasInvalidOperationException">The header can not be completed.</exception>
        /// <returns></returns>
        public ValueTask CompleteWritingAsync(Guid fileId, Guid versionId, long length, byte[] hash, CancellationToken cancellationToken);
        
        /// <summary>
        /// Cancels the writing process.
        /// </summary>
        /// <param name="fileId">The file id.</param>
        /// /// <param name="versionId">The versionId id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentException">Invalid parameters.</exception>
        /// <exception cref="FaasFileNotFoundException">Invalid id.</exception>
        /// <exception cref="FaasInvalidOperationException">The header can not be completed.</exception>
        /// <returns></returns>
        public Task CancelWritingAsync(Guid fileId, Guid versionId, CancellationToken cancellationToken);
    }
}