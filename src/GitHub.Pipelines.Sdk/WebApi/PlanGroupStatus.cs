using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
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
