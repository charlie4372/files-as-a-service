using System;

namespace FilesAsAService
{
    public class FaasFileHeaderVersion
    {
        public Guid VersionId { get; set; }
        
        public long Length { get; set; }
        
        public byte[] Hash { get; set; }
        
        public DateTime DateCreatedUtc { get; set; }
        
        public DateTime? DateDeletedUtc { get; set; }
        
        public FaasFileHeaderVersionStatus Status { get; set; }

        public FaasFileHeaderVersion()
        {
            Hash = new byte[0];
        }

        public FaasFileHeaderVersion(FaasFileHeaderVersion copyFrom)
        {
            VersionId = copyFrom.VersionId;
            Length = copyFrom.Length;
            Hash = copyFrom.Hash;
            DateCreatedUtc = copyFrom.DateCreatedUtc;
            DateDeletedUtc = copyFrom.DateDeletedUtc;
            Status = copyFrom.Status;
        }
    }
}