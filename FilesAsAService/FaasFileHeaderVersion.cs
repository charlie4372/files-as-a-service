using System;

namespace FilesAsAService
{
    public class FaasFileHeaderVersion
    {
        public Guid Id { get; set; }
        
        public long Length { get; set; }
        
        public byte[] Hash { get; set; }
        
        public DateTime DateCreatedUtc { get; set; }
        
        public DateTime? DateDeletedUtc { get; set; }
        
        public FileHeaderVersionStatus Status { get; set; }

        public FaasFileHeaderVersion()
        {
        }

        public FaasFileHeaderVersion(FaasFileHeaderVersion copyFrom)
        {
            Id = copyFrom.Id;
            Length = copyFrom.Length;
            Hash = copyFrom.Hash;
            DateCreatedUtc = copyFrom.DateCreatedUtc;
            DateDeletedUtc = copyFrom.DateDeletedUtc;
            Status = copyFrom.Status;
        }
    }
}