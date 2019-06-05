using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    internal enum RollingDeploymentOption
    {
        [EnumMember]
        Absolute,

        [EnumMember]
        Percentage
    }
}
