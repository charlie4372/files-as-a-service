using System;

namespace FilesAsAService
{
    public struct FaasFileVersionId
    {
        public Guid FileId { get; }
        
        public Guid VersionId { get; }

        public FaasFileVersionId(Guid fileId, Guid versionId)
        {
            FileId = fileId;
            VersionId = versionId;
        }
    }
}