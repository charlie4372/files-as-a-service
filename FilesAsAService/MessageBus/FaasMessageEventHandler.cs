namespace FilesAsAService.MessageBus
{
    public delegate void FaasMessageEventHandler(object sender, FaasMessageEventArgs args);
}