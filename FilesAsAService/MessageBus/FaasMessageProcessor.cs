using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.MessageBus
{
    public class FaasMessageProcessor : IFaasMessageProcessor
    {
        private readonly IList<FaasContainer> _containers;

        public FaasMessageProcessor(IList<FaasContainer> containers)
        {
            _containers = containers ?? throw new ArgumentNullException(nameof(containers));
        }
        
        public async Task Process(FaasDeleteFileVersionMessageV1 message, CancellationToken cancellationToken)
        {
            var container = GetContainer(message.Container);
            if (container == null)
                // TODO better error.
                throw new Exception("Invalid container.");

            var fileExists = await container.HasFileAsync(message.FileId, message.VersionId, cancellationToken);
            if (!fileExists)
                return;
            
            if (cancellationToken.IsCancellationRequested)
                cancellationToken.ThrowIfCancellationRequested();

            await container.DeleteFileAsync(message.FileId, message.VersionId, cancellationToken);
        }

        private FaasContainer? GetContainer(string name)
        {
            return _containers.FirstOrDefault(container => container.Name == name);
        }
    }
}