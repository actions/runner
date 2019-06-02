using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAgentQueueStore
    {
        /// <summary>
        /// Adds a reference which should be considered authorized. Future
        /// calls to retrieve this resource will be treated as pre-authorized regardless
        /// of authorization context used.
        /// </summary>
        /// <param name="reference">The queue which should be authorized</param>
        void Authorize(IList<TaskAgentQueue> queues);
        
        IList<AgentQueueReference> GetAuthorizedReferences();

        TaskAgentQueue Get(AgentQueueReference reference);

        /// <summary>
        /// Gets the <c>IAgentQueueResolver</c> used by this store, if any.
        /// </summary>
        IAgentQueueResolver Resolver { get; }
    }
}
