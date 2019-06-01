using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.DelegatedAuthorization
{
    [DataContract]
    public enum SortByOptions
    {
        [EnumMember]
        DisplayName = 1,

        [EnumMember]
        DisplayDate = 2,

        [EnumMember]
        Status = 3
    }
}
