using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism of resolving an <c>AgentQueueReference</c> to a <c>TaskAgentQueue</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAgentQueueResolver
    {
        /// <summary>
        /// Attempts to resolve the agent queue references to <c>TaskAgentQueue</c> instances.
        /// </summary>
        /// <param name="references">The agent queues which should be resolved</param>
        /// <returns>A list containing the resolved agent queues</returns>
        IList<TaskAgentQueue> Resolve(ICollection<AgentQueueReference> references);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IAgentQueueResolverExtensions
    {
        /// <summary>
        /// Attempts to resolve the agent queue reference to a <c>TaskAgentQueue</c>.
        /// </summary>
        /// <param name="reference">The agent queue which should be resolved</param>
        /// <returns>The agent queue if resolved; otherwise, null</returns>
        public static TaskAgentQueue Resolve(
            this IAgentQueueResolver resolver,
            AgentQueueReference reference)
        {
            return resolver.Resolve(new[] { reference }).FirstOrDefault();
        }
    }
}
