using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [Flags]
    [DataContract]
    public enum PlanGroupStatus
    {
        [EnumMember]
        Running = 1,

        [EnumMember]
        Queued = 2,

        [EnumMember]
        All = Running | Queued
    }
}
