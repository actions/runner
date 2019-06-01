using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Filters queues based on whether the calling user has permission to use or manage the queue.
    /// </summary>
    [Flags]
    [DataContract]
    public enum TaskAgentQueueActionFilter
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        Manage = 2,

        [EnumMember]
        Use = 16,
    }
}
