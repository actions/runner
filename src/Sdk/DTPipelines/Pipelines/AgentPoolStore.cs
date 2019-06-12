using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AgentPoolStore : IAgentPoolStore
    {
        public AgentPoolStore(
            IList<TaskAgentPool> pools,
            IAgentPoolResolver resolver = null)
        {
            this.Resolver = resolver;
            Add(pools?.ToArray());
        }

        /// <summary>
        /// Get the queue resolver configured for this store.
        /// </summary>
        public IAgentPoolResolver Resolver
        {
            get;
        }

        public void Authorize(IList<AgentPoolReference> pools)
        {
            if (pools?.Count > 0)
            {
                foreach (var pool in pools)
                {
                    var authorizedResource = this.Resolver?.Resolve(pool);
                    if (authorizedResource != null)
                    {
                        Add(authorizedResource);
                    }
                }
            }
        }

        public IList<AgentPoolReference> GetAuthorizedReferences()
        {
            return m_resourcesById.Values.Select(x => new AgentPoolReference { Id = x.Id }).ToList();
        }

        public TaskAgentPool Get(AgentPoolReference reference)
        {
            if (reference == null)
            {
                return null;
            }

            var referenceId = reference.Id;
            var referenceName = reference.Name?.Literal;
            if (reference.Id == 0 && String.IsNullOrEmpty(referenceName))
            {
                return null;
            }

            TaskAgentPool authorizedResource = null;
            if (referenceId != 0)
            {
                if (m_resourcesById.TryGetValue(referenceId, out authorizedResource))
                {
                    return authorizedResource;
                }
            }
            else if (!String.IsNullOrEmpty(referenceName))
            {
                if (m_resourcesByName.TryGetValue(referenceName, out authorizedResource))
                {
                    return authorizedResource;
                }
            }

            // If we have an authorizer then attempt to authorize the reference for use
            authorizedResource = this.Resolver?.Resolve(reference);
            if (authorizedResource != null)
            {
                Add(authorizedResource);
            }

            return authorizedResource;
        }

        private void Add(params TaskAgentPool[] resources)
        {
            if (resources?.Length > 0)
            {
                foreach (var resource in resources)
                {
                    // Track by ID
                    if (m_resourcesById.TryGetValue(resource.Id, out _))
                    {
                        continue;
                    }

                    m_resourcesById.Add(resource.Id, resource);

                    // Track by name
                    if (m_resourcesByName.TryGetValue(resource.Name, out _))
                    {
                        continue;
                    }

                    m_resourcesByName.Add(resource.Name, resource);
                }
            }
        }

        private readonly Dictionary<Int32, TaskAgentPool> m_resourcesById = new Dictionary<Int32, TaskAgentPool>();
        private readonly Dictionary<String, TaskAgentPool> m_resourcesByName = new Dictionary<String, TaskAgentPool>(StringComparer.OrdinalIgnoreCase);
    }
}
