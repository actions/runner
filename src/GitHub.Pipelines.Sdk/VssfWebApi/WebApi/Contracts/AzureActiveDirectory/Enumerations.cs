using System.Runtime.Serialization;

namespace GitHub.Services.AadMemberAccessStatus
{
    [DataContract]
    public enum AadMemberAccessState
    {
        [EnumMember]
        Indeterminate = -1,

        [EnumMember]
        Deleted = 0,

        [EnumMember]
        Disabled = 1,

        [EnumMember]
        Invalid = 2,

        [EnumMember]
        Valid = 3
    }
}
