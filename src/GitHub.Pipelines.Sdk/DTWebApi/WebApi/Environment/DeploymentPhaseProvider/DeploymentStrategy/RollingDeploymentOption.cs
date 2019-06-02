using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    internal enum RollingDeploymentOption
    {
        [EnumMember]
        Absolute,

        [EnumMember]
        Percentage
    }
}