using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// This is useful in getting a list of deployment targets, filtered by the deployment agent status.
    /// </summary>
    [Flags]
    [DataContract]
    public enum TaskAgentStatusFilter
    {
        /// <summary>
        /// Only deployment targets that are offline.
        /// </summary>
        [EnumMember]
        Offline = 1,

        /// <summary>
        /// Only deployment targets that are online.
        /// </summary>
        [EnumMember]
        Online = 2,

        /// <summary>
        /// All deployment targets.
        /// </summary>
        [EnumMember]
        All = Offline | Online
    }
}
