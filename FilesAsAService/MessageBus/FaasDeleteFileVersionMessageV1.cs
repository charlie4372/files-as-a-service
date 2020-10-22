using System;

namespace FilesAsAService.MessageBus
{
    public class FaasDeleteFileVersionMessageV1 : FaasMessage
    {
        public string Container { get; set; } = "";
        
        public Guid FileId { get; set; }
        
        public Guid VersionId { get; set; }

        public FaasDeleteFileVersionMessageV1()
        {
            Version = 1;
            Name = "delete-file";
        }

        public FaasDeleteFileVersionMessageV1(string container, Guid fileId, Guid versionId) : this()
        {
            DateCreatedUtc = DateTime.UtcNow;
            Container = container;
            FileId = fileId;
            VersionId = versionId;
        }
    }
}