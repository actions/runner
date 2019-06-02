using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [Flags]
    [DataContract]
	//[Obsolete] -- Mark as obsolete in M118.
    public enum PlanGroupStatusFilter
    {
        [EnumMember]
        Running = 1,

        [EnumMember]
        Queued = 2,

        [EnumMember]
        All = Running | Queued
    }
}
