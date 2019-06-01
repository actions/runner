using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    [DataContract]
    public enum DisplayFilterOptions
    {
        [EnumMember]
        Active = 1,

        [EnumMember]
        Revoked = 2,

        [EnumMember]
        Expired = 3,

        [EnumMember]
        All = 4
    }
}

