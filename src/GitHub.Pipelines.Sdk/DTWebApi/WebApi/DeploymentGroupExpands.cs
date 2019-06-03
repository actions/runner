using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Properties to be included or expanded in deployment group objects. This is useful when getting a single or list of deployment grouops.
    /// </summary>
    [Flags]
    [DataContract]
    public enum DeploymentGroupExpands
    {
        /// <summary>
        /// No additional properties.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Deprecated: Include all the deployment targets.
        /// </summary>
        [EnumMember]
        Machines = 2,

        /// <summary>
        /// Include unique list of tags across all deployment targets. 
        /// </summary>
        [EnumMember]
        Tags = 4
    }
}
