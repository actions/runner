using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Filters pools based on whether the calling user has permission to use or manage the pool.
    /// </summary>
    [Flags]
    [DataContract]
    public enum TaskAgentPoolActionFilter
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        Manage = 2,

        [EnumMember]
        Use = 16,
    }
}
