using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Properties to be included or expanded in deployment pool summary objects. This is useful when getting a single or list of deployment pool summaries.
    /// </summary>
    [Flags]
    [DataContract]
    public enum DeploymentPoolSummaryExpands
    {
        /// <summary>
        /// No additional properties
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Include deployment groups referring to the deployment pool.
        /// </summary>
        [EnumMember]
        DeploymentGroups = 2,

        /// <summary>
        /// Include Resource referring to the deployment pool.
        /// </summary>
        [EnumMember]
        Resource = 4
    }
}
