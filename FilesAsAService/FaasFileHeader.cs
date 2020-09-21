using System;

namespace FilesAsAService
{
    public class FaasFileHeader
    {
        public Guid Id { get; set; }
        
        public string Name { get; set; }
        
        public long Length { get; set; }
        
        public DateTime DateCreatedUtc { get; set; }
        
        public DateTime DateUpdatedUtc { get; set; }
        
        public DateTime? DateDeletedUtc { get; set; }
        
        public byte[] Hash { get; set; }
        
        public FaasFileHeaderStatus Status { get; set; }

        public FaasFileHeader()
        {
        }
        
        public FaasFileHeader(FaasFileHeader copyFrom)
        {
            Id = copyFrom.Id;
            Name = copyFrom.Name;
            Length = copyFrom.Length;
            DateCreatedUtc = copyFrom.DateCreatedUtc;
            DateUpdatedUtc = copyFrom.DateUpdatedUtc;
            DateDeletedUtc = copyFrom.DateDeletedUtc;
            Hash = copyFrom.Hash;
            Status = copyFrom.Status;
        }
    }
}