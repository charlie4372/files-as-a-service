using System;
using MongoDB.Bson.Serialization.Attributes;

namespace FilesAsAService.MongoDb.Models
{
    public class FaasFileHeaderVersionMongoEntity
    {
        [BsonRequired]
        public Guid VersionId { get; set; }

        public byte[] Hash { get; set; }
        
        public long Length { get; set; }
        
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime DateCreatedUtc { get; set; }
        
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? DateDeletedUtc { get; set; }
        
        public FaasFileHeaderVersionStatus Status { get; set; }

        public FaasFileHeaderVersionMongoEntity()
        {
        }

        public FaasFileHeaderVersionMongoEntity(FaasFileHeaderVersion headerVersion)
        {
            VersionId = headerVersion.VersionId;
            Length = headerVersion.Length;
            Hash = headerVersion.Hash;
            DateCreatedUtc = headerVersion.DateCreatedUtc;
            DateDeletedUtc = headerVersion.DateDeletedUtc;
            Status = headerVersion.Status;
        }

        public FaasFileHeaderVersion ToFaasFileHeaderVersion()
        {
            return new FaasFileHeaderVersion
            {
                VersionId = VersionId,
                Length = Length,
                Hash = Hash,
                DateCreatedUtc = DateCreatedUtc,
                DateDeletedUtc = DateDeletedUtc,
                Status = Status,
            };
        }
    }
}