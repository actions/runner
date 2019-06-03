using System.Runtime.Serialization;

namespace GitHub.Services.DelegatedAuthorization
{
    [DataContract]
    public enum CreatedByOptions
    {
        [EnumMember]
        VstsWebUi = 1,

        [EnumMember]
        NonVstsWebUi = 2,

        [EnumMember]
        All = 3
    }
}
