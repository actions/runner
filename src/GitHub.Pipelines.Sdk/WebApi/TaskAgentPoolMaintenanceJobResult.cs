using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public enum TaskAgentPoolMaintenanceJobResult
    {
        [EnumMember]
        Succeeded = 1,

        [EnumMember]
        Failed = 2,

        [EnumMember]
        Canceled = 4,
    }
}
