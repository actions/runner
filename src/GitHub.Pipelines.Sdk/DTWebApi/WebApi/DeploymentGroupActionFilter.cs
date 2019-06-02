using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// This is useful in getting a list of deployment groups, filtered for which caller has permissions to take a particular action.
    /// </summary>
    [Flags]
    [DataContract]
    public enum DeploymentGroupActionFilter
    {
        /// <summary>
        /// All deployment groups.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Only deployment groups for which caller has **manage** permission.
        /// </summary>
        [EnumMember]
        Manage = 2,

        /// <summary>
        /// Only deployment groups for which caller has **use** permission.
        /// </summary>
        [EnumMember]
        Use = 16,
    }
}
