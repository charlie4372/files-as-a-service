using System.Threading.Tasks;

namespace FilesAsAService.MessageBus
{
    public interface IFaasMessageBus
    {
        Task Send(FaasDeleteFileVersionMessageV1 versionMessage);

        event FaasMessageEventHandler MessageProcessed;
    }
}