using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IAgentPoolStore
    {
        /// <summary>
        /// Adds a reference which should be considered authorized. Future
        /// calls to retrieve this resource will be treated as pre-authorized regardless
        /// of authorization context used.
        /// </summary>
        /// <param name="pools">The pools which should be authorized</param>
        void Authorize(IList<AgentPoolReference> pools);

        IList<AgentPoolReference> GetAuthorizedReferences();

        TaskAgentPool Get(AgentPoolReference reference);

        /// <summary>
        /// Gets the <c>IAgentPoolResolver</c> used by this store, if any.
        /// </summary>
        IAgentPoolResolver Resolver { get; }
    }
}
