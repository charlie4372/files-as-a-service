using System;

namespace FilesAsAService.MessageBus
{
    public interface IFaasMessage
    {
        int Version { get; }
        
        string Name { get; }
        
        DateTime DateCreatedUtc { get; }
    }
}