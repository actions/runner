using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum TimelineRecordState
    {
        [EnumMember]
        Pending,

        [EnumMember]
        InProgress,

        [EnumMember]
        Completed,
    }
}
