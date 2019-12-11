using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// The type of agent pool.
    /// </summary>
    [DataContract]
    public enum TaskAgentPoolType
    {
        /// <summary>
        /// A typical pool of task agents
        /// </summary>
        [EnumMember]
        Automation = 1
    }
}
