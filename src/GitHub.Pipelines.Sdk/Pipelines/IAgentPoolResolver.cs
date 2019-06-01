using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a mechanism of resolving an <c>AgentPoolReference</c> to a <c>TaskAgentPool</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAgentPoolResolver
    {
        /// <summary>
        /// Attempts to resolve the agent pool references to <c>TaskAgentPool</c> instances.
        /// </summary>
        /// <param name="references">The agent pools which should be resolved</param>
        /// <returns>A list containing the resolved agent pools</returns>
        IList<TaskAgentPool> Resolve(ICollection<AgentPoolReference> references);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IAgentPoolResolverExtensions
    {
        /// <summary>
        /// Attempts to resolve the agent pool reference to a <c>TaskAgentPool</c>.
        /// </summary>
        /// <param name="reference">The agent pool which should be resolved</param>
        /// <returns>The agent pool if resolved; otherwise, null</returns>
        public static TaskAgentPool Resolve(
            this IAgentPoolResolver resolver,
            AgentPoolReference reference)
        {
            return resolver.Resolve(new[] { reference }).FirstOrDefault();
        }
    }
}
