using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
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
