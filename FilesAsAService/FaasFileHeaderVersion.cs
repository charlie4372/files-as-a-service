using System;

namespace FilesAsAService
{
    public class FaasFileHeaderVersion
    {
        private static readonly byte[] EmptyHash = new byte[0];
        
        private long _length;
        private int _blockSize;
        private int? _numberOfBlocks = null;

        public Guid VersionId { get; set; }

        public long Length
        {
            get => _length;
            set
            {
                _length = value;
                _numberOfBlocks = null;
            }
        }

        public byte[] Hash { get; set; }
        
        public DateTime DateCreatedUtc { get; set; }
        
        public DateTime? DateDeletedUtc { get; set; }
        
        public FaasFileHeaderVersionStatus Status { get; set; }

        public FaasFileHeaderVersion()
        {
            Hash = EmptyHash;
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