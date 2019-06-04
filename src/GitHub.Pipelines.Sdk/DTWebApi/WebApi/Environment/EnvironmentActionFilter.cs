using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// This is useful in getting a list of Environments, filtered for which caller has permissions to take a particular action.
    /// </summary>
    [Flags]
    [DataContract]
    public enum EnvironmentActionFilter
    {
        /// <summary>
        /// All environments for which user has **view** permission.
        /// </summary>
        [EnumMember]
        None = 0,

        /// <summary>
        /// Only environments for which caller has **manage** permission.
        /// </summary>
        [EnumMember]
        Manage = 2,

        /// <summary>
        /// Only environments for which caller has **use** permission.
        /// </summary>
        [EnumMember]
        Use = 16
    }
}
