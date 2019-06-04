using System.Runtime.Serialization;

namespace GitHub.Services.GitHubConnector
{
    [DataContract]
    public enum GitHubAccountType
    {
        [EnumMember]
        Organization = 0,

        [EnumMember]
        User = 1,
    }
}
