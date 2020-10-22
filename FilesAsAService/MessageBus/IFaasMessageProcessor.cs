using System.Threading;
using System.Threading.Tasks;

namespace FilesAsAService.MessageBus
{
    public interface IFaasMessageProcessor
    {
        Task Process(FaasDeleteFileVersionMessageV1 message, CancellationToken cancellationToken);
    }
}