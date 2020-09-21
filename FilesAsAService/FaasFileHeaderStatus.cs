using System.Runtime.Serialization;

namespace FilesAsAService
{
    public enum FaasFileHeaderStatus
    {
        [EnumMember(Value = "creating")]
        Creating,
        
        [EnumMember(Value = "active")]
        Active
    }
}