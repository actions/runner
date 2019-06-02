using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
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
