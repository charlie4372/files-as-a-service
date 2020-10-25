using System;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;

namespace FilesAsAService.MongoDb.Models
{
    public class FaasFileHeaderMongoEntity
    {
        [BsonId]
        public Guid FileId { get; set; }

        [BsonRequired]
        public string Name { get; set; }
        
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime DateCreatedUtc { get; set; }
        
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? DateDeletedUtc { get; set; }
        
        public Guid? VersionId { get; set; }
        
        public FaasFileHeaderVersionMongoEntity[] Versions { get; set; }
        
        public FaasFileHeaderMongoEntity()
        {
        }

        public FaasFileHeaderMongoEntity(FaasFileHeader header)
        {
            FileId = header.FileId;
            Name = header.Name;
            DateCreatedUtc = header.DateCreatedUtc;
            DateDeletedUtc = header.DateDeletedUtc;
            VersionId = header.VersionId;
            Versions = header.Versions.Select(row => new FaasFileHeaderVersionMongoEntity(row)).ToArray();
        }

        public FaasFileHeader ToFaasFileHeader()
        {
            return new FaasFileHeader
            {
                FileId = FileId,
                Name = Name,
                DateCreatedUtc = DateCreatedUtc,
                DateDeletedUtc = DateDeletedUtc,
                VersionId = VersionId,
                Versions = Versions.Select(row => row.ToFaasFileHeaderVersion()).ToArray()
            };
        }
    }
}