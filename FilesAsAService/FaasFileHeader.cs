using System;
using System.Linq;

namespace FilesAsAService
{
    public class FaasFileHeader
    {
        public Guid FileId { get; set; }
        
        public string Name { get; set; }
        
        public DateTime DateCreatedUtc { get; set; }
        
        public DateTime? DateDeletedUtc { get; set; }
        
        public Guid? VersionId { get; set; }
        
        public FaasFileHeaderVersion[] Versions { get; set; }

        public FaasFileHeader()
        {
            Versions = new FaasFileHeaderVersion[0];
        }
        
        public FaasFileHeader(FaasFileHeader copyFrom)
        {
            FileId = copyFrom.FileId;
            Name = copyFrom.Name;
            DateCreatedUtc = copyFrom.DateCreatedUtc;
            DateDeletedUtc = copyFrom.DateDeletedUtc;
            VersionId = copyFrom.VersionId;
            Versions = copyFrom.Versions.Select(v => new FaasFileHeaderVersion(v)).ToArray();
        }
    }
}