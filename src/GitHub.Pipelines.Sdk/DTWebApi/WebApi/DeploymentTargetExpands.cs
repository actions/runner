using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Properties to be included or expanded in deployment target objects. This is useful when getting a single or list of deployment targets.
    /// </summary>
    [Flags]
    [DataContract]
    public enum DeploymentTargetExpands
    {
        /// <summary>
        /// No additional properties.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Include capabilities of the deployment agent.
        /// </summary>
        [EnumMember]
        Capabilities = 2,

        /// <summary>
        /// Include the job request assigned to the deployment agent.
        /// </summary>
        [EnumMember]
        AssignedRequest = 4,

        /// <summary>
        /// Include the last completed job request of the deployment agent.
        /// </summary>
        [EnumMember]
        LastCompletedRequest = 8
    }
}
