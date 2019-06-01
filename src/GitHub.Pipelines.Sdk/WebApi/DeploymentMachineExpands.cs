using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [Flags]
    [DataContract]
    public enum DeploymentMachineExpands
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        Capabilities = 2,

        [EnumMember]
        AssignedRequest = 4
    }
}
