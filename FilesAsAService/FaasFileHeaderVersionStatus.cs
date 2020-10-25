using System.Runtime.Serialization;

namespace FilesAsAService
{
    public enum FaasFileHeaderVersionStatus
    {
        [EnumMember(Value = "ok")]
        Ok = 1,
        
        [EnumMember(Value = "writing")]
        Writing = 2,
        
        [EnumMember(Value = "soft-delete")]
        SoftDelete = 4,
        
        [EnumMember(Value = "hard-delete")]
        HardDelete = 8
    }
}