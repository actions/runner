using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskAgentPoolMaintenanceJobStatus
    {
        [EnumMember]
        InProgress = 1,

        [EnumMember]
        Completed = 2,

        [EnumMember]
        Cancelling = 4,

        [EnumMember]
        Queued = 8,
    }
}
