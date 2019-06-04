using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskResult
    {
        [EnumMember]
        Succeeded = 0,

        [EnumMember]
        SucceededWithIssues = 1,

        [EnumMember]
        Failed = 2,

        [EnumMember]
        Canceled = 3,

        [EnumMember]
        Skipped = 4,

        [EnumMember]
        Abandoned = 5,
    }
}
