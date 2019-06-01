using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class AgentQueueStore : IAgentQueueStore
    {
        public AgentQueueStore(
            IList<TaskAgentQueue> queues,
            IAgentQueueResolver resolver = null)
        {
            this.Resolver = resolver;
            Add(queues?.ToArray());
        }

        /// <summary>
        /// Get the queue resolver configured for this store.
        /// </summary>
        public IAgentQueueResolver Resolver
        {
            get;
        }

        public void Authorize(IList<TaskAgentQueue> queues)
        {
            if (queues?.Count > 0)
            {
                foreach (var queue in queues)
                {
                    Add(queue);
                }
            }
        }

        public IList<AgentQueueReference> GetAuthorizedReferences()
        {
            return m_resourcesById.Values.Select(x => new AgentQueueReference { Id = x.Id }).ToList();
        }

        public TaskAgentQueue Get(AgentQueueReference reference)
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

            TaskAgentQueue authorizedResource = null;
            if (referenceId != 0)
            {
                if (m_resourcesById.TryGetValue(referenceId, out authorizedResource))
                {
                    return authorizedResource;
                }
            }
            else if (!String.IsNullOrEmpty(referenceName))
            {
                if (m_resourcesByName.TryGetValue(referenceName, out List<TaskAgentQueue> matchingResources))
                {
                    if (matchingResources.Count > 1)
                    {
                        throw new AmbiguousResourceSpecificationException(PipelineStrings.AmbiguousServiceEndpointSpecification(referenceId));
                    }

                    return matchingResources[0];
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

        private void Add(params TaskAgentQueue[] resources)
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

                    // not all references have names
                    var name = resource.Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    // Track by name
                    if (!m_resourcesByName.TryGetValue(name, out var list))
                    {
                        list = new List<TaskAgentQueue>();
                        m_resourcesByName.Add(name, list);
                    }

                    // Clobber previously added alternate name, with the real hosted queue.
                    // For example, during the "Hosted macOS High Sierra" transition, until the real queue
                    // existed, it was treated as an alternate name for the "Hosted macOS" queue. After the
                    // real "Hosted macOS High Sierra" queue was created, it took priority.
                    if (list.Count > 0 && list[0].Pool?.IsHosted == true && resource.Pool?.IsHosted == true)
                    {
                        list[0] = resource;
                    }
                    // Otherwise add the queue
                    else
                    {
                        list.Add(resource);
                    }

                    // Track by alternate name for specific hosted pools.
                    // For example, "Hosted macOS Preview" and "Hosted macOS" are equivalent.
                    if (resource.Pool?.IsHosted == true && s_alternateNames.TryGetValue(name, out var alternateNames))
                    {
                        foreach (var alternateName in alternateNames)
                        {
                            if (!m_resourcesByName.TryGetValue(alternateName, out list))
                            {
                                list = new List<TaskAgentQueue>();
                                m_resourcesByName.Add(alternateName, list);
                            }

                            if (list.Count == 0 || list[0].Pool?.IsHosted != true)
                            {
                                list.Add(resource);
                            }
                        }
                    }
                }
            }
        }

        private static readonly Dictionary<String, String[]> s_alternateNames = new Dictionary<String, String[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Hosted macOS", new[] { "Hosted macOS Preview" } },
            { "Hosted macOS Preview", new[] { "Hosted macOS" } },
        };
        private readonly Dictionary<Int32, TaskAgentQueue> m_resourcesById = new Dictionary<Int32, TaskAgentQueue>();
        private readonly Dictionary<String, List<TaskAgentQueue>> m_resourcesByName = new Dictionary<String, List<TaskAgentQueue>>(StringComparer.OrdinalIgnoreCase);
    }
}
