using System.Runtime.Serialization;

namespace FilesAsAService
{
    public enum FileHeaderVersionStatus
    {
        [EnumMember(Value = "ok")]
        Ok,
        
        [EnumMember(Value = "writing")]
        Writing,
        
        [EnumMember(Value = "soft-delete")]
        SoftDelete,
        
        [EnumMember(Value = "hard-delete")]
        HardDelete
    }
}