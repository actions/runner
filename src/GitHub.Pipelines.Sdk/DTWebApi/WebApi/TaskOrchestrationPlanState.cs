﻿using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskOrchestrationPlanState
    {
        [EnumMember]
        InProgress = 1,

        [EnumMember]
        Queued = 2,

        [EnumMember]
        Completed = 4,

        [EnumMember]
        Throttled = 8
    }
}
