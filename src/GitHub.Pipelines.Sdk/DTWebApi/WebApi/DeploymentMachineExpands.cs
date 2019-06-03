using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
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
