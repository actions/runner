using System.Runtime.Serialization;

namespace GitHub.Services.DelegatedAuthorization
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
