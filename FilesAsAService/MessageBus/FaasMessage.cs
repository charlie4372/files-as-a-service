using System;

namespace FilesAsAService.MessageBus
{
    public class FaasMessage : IFaasMessage
    {
        public int Version { get; set; }
        public string Name { get; set; }
        public DateTime DateCreatedUtc { get; set; }
    }
}