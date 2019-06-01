using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public enum IssueType
    {
        [EnumMember]
        Error = 1,

        [EnumMember]
        Warning = 2
    }
}
