using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides collections of securable resources available for use within a pipeline.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineResources
    {
        /// <summary>
        /// Initializes a new <c>PipelineResources</c> instance with empty resource collections.
        /// </summary>
        public PipelineResources()
        {
        }

        private PipelineResources(PipelineResources resourcesToCopy)
        {
            if (resourcesToCopy.m_builds?.Count > 0)
            {
                m_builds = new HashSet<BuildResource>(resourcesToCopy.m_builds.Select(x => x.Clone()), new ResourceComparer());
            }

            if (resourcesToCopy.m_containers?.Count > 0)
            {
                m_containers = new HashSet<ContainerResource>(resourcesToCopy.m_containers.Select(x => x.Clone()), new ResourceComparer());
            }

            if (resourcesToCopy.m_endpoints?.Count > 0)
            {
                m_endpoints = new HashSet<ServiceEndpointReference>(resourcesToCopy.m_endpoints.Select(x => x.Clone()), new EndpointComparer());
            }

            if (resourcesToCopy.m_environments?.Count > 0)
            {
                m_environments = new HashSet<EnvironmentReference>(resourcesToCopy.m_environments.Select(x => x.Clone()), new EnvironmentComparer());
            }

            if (resourcesToCopy.m_files?.Count > 0)
            {
                m_files = new HashSet<SecureFileReference>(resourcesToCopy.m_files.Select(x => x.Clone()), new FileComparer());
            }

            if (resourcesToCopy.m_pipelines?.Count > 0)
            {
                m_pipelines = new HashSet<PipelineResource>(resourcesToCopy.m_pipelines.Select(x => x.Clone()), new ResourceComparer());
            }

            if (resourcesToCopy.m_queues?.Count > 0)
            {
                m_queues = new HashSet<AgentQueueReference>(resourcesToCopy.m_queues.Select(x => x.Clone()), new QueueComparer());
            }

            if (resourcesToCopy.m_pools?.Count > 0)
            {
                m_pools = new HashSet<AgentPoolReference>(resourcesToCopy.m_pools.Select(x => x.Clone()), new PoolComparer());
            }

            if (resourcesToCopy.m_repositories?.Count > 0)
            {
                m_repositories = new HashSet<RepositoryResource>(resourcesToCopy.m_repositories.Select(x => x.Clone()), new ResourceComparer());
            }

            if (resourcesToCopy.m_variableGroups?.Count > 0)
            {
                m_variableGroups = new HashSet<VariableGroupReference>(resourcesToCopy.m_variableGroups.Select(x => x.Clone()), new VariableGroupComparer());
            }
        }

        /// <summary>
        /// Gets the total count of resources.
        /// </summary>
        public Int32 Count => (m_builds?.Count ?? 0) +
                              (m_containers?.Count ?? 0) +
                              (m_endpoints?.Count ?? 0) +
                              (m_environments?.Count ?? 0) +
                              (m_files?.Count ?? 0) +
                              (m_pipelines?.Count ?? 0) +
                              (m_queues?.Count ?? 0) +
                              (m_pools?.Count ?? 0) +
                              (m_repositories?.Count ?? 0) +
                              (m_variableGroups?.Count ?? 0);

        /// <summary>
        /// List of all resources that need to be sent to PolicyService
        /// </summary>
        public IEnumerable<ResourceReference> GetSecurableResources()
        {
            foreach (var resourceCollection in new IEnumerable<ResourceReference>[] {
                m_endpoints,
                m_environments,
                m_files,
                m_queues,
                m_pools,
                m_variableGroups
            })
            {
                if (resourceCollection != null)
                {
                    foreach (var r in resourceCollection)
                    {
                        if (r != null)
                        {
                            yield return r;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the collection of build resources defined in the pipeline.
        /// </summary>
        public ISet<BuildResource> Builds
        {
            get
            {
                if (m_builds == null)
                {
                    m_builds = new HashSet<BuildResource>(new ResourceComparer());
                }
                return m_builds;
            }
        }

        /// <summary>
        /// Gets the collection of container resources defined in the pipeline.
        /// </summary>
        public ISet<ContainerResource> Containers
        {
            get
            {
                if (m_containers == null)
                {
                    m_containers = new HashSet<ContainerResource>(new ResourceComparer());
                }
                return m_containers;
            }
        }

        /// <summary>
        /// Gets the collection of endpoint references available in the resources of a pipeline.
        /// </summary>
        public ISet<ServiceEndpointReference> Endpoints
        {
            get
            {
                if (m_endpoints == null)
                {
                    m_endpoints = new HashSet<ServiceEndpointReference>(new EndpointComparer());
                }
                return m_endpoints;
            }
        }

        /// <summary>
        /// Gets the collection of environments listed with deployment job in pipeline.
        /// </summary>
        public ISet<EnvironmentReference> Environments
        {
            get
            {
                if (m_environments == null)
                {
                    m_environments = new HashSet<EnvironmentReference>(new EnvironmentComparer());
                }
                return m_environments;
            }
        }

        /// <summary>
        /// Gets the collection of secure file references available in the resources of a pipeline.
        /// </summary>
        public ISet<SecureFileReference> Files
        {
            get
            {
                if (m_files == null)
                {
                    m_files = new HashSet<SecureFileReference>(new FileComparer());
                }
                return m_files;
            }
        }

        /// <summary>
        /// Gets the collection of pipeline resources defined in the pipeline.
        /// </summary>
        public ISet<PipelineResource> Pipelines
        {
            get
            {
                if (m_pipelines == null)
                {
                    m_pipelines = new HashSet<PipelineResource>(new ResourceComparer());
                }
                return m_pipelines;
            }
        }

        /// <summary>
        /// Gets the collection of agent queue references available in the resources of a pipeline.
        /// </summary>
        public ISet<AgentQueueReference> Queues
        {
            get
            {
                if (m_queues == null)
                {
                    m_queues = new HashSet<AgentQueueReference>(new QueueComparer());
                }
                return m_queues;
            }
        }

        /// <summary>
        /// Gets the collection of agent pool references available in the resources of a pipeline.
        /// </summary>
        public ISet<AgentPoolReference> Pools
        {
            get
            {
                if (m_pools == null)
                {
                    m_pools = new HashSet<AgentPoolReference>(new PoolComparer());
                }
                return m_pools;
            }
        }

        /// <summary>
        /// Gets the collection of repository resources defined in the pipeline.
        /// </summary>
        public ISet<RepositoryResource> Repositories
        {
            get
            {
                if (m_repositories == null)
                {
                    m_repositories = new HashSet<RepositoryResource>(new ResourceComparer());
                }
                return m_repositories;
            }
        }

        /// <summary>
        /// Gets the collection of variable group references available in the resources of a pipeline.
        /// </summary>
        public ISet<VariableGroupReference> VariableGroups
        {
            get
            {
                if (m_variableGroups == null)
                {
                    m_variableGroups = new HashSet<VariableGroupReference>(new VariableGroupComparer());
                }
                return m_variableGroups;
            }
        }

        public PipelineResources Clone()
        {
            return new PipelineResources(this);
        }

        public void MergeWith(PipelineResources resources)
        {
            if (resources != null)
            {
                this.Builds.UnionWith(resources.Builds);
                this.Containers.UnionWith(resources.Containers);
                this.Endpoints.UnionWith(resources.Endpoints);
                this.Environments.UnionWith(resources.Environments);
                this.Files.UnionWith(resources.Files);
                this.Pipelines.UnionWith(resources.Pipelines);
                this.Queues.UnionWith(resources.Queues);
                this.Pools.UnionWith(resources.Pools);
                this.Repositories.UnionWith(resources.Repositories);
                this.VariableGroups.UnionWith(resources.VariableGroups);
            }
        }

        internal void AddEndpointReference(String endpointId)
        {
            if (Guid.TryParse(endpointId, out Guid endpointIdValue))
            {
                this.Endpoints.Add(new ServiceEndpointReference { Id = endpointIdValue });
            }
            else
            {
                this.Endpoints.Add(new ServiceEndpointReference { Name = endpointId });
            }
        }

        internal void AddEndpointReference(ServiceEndpointReference reference)
        {
            this.Endpoints.Add(reference);
        }

        internal void AddSecureFileReference(String fileId)
        {
            if (Guid.TryParse(fileId, out Guid fileIdValue))
            {
                this.Files.Add(new SecureFileReference { Id = fileIdValue });
            }
            else
            {
                this.Files.Add(new SecureFileReference { Name = fileId });
            }
        }

        internal void AddSecureFileReference(SecureFileReference reference)
        {
            this.Files.Add(reference);
        }

        internal void AddAgentQueueReference(AgentQueueReference reference)
        {
            this.Queues.Add(reference);
        }

        internal void AddAgentPoolReference(AgentPoolReference reference)
        {
            this.Pools.Add(reference);
        }

        internal void AddVariableGroupReference(VariableGroupReference reference)
        {
            this.VariableGroups.Add(reference);
        }

        internal void AddEnvironmentReference(EnvironmentReference reference)
        {
            this.Environments.Add(reference);
        }

        internal void Clear()
        {
            m_builds?.Clear();
            m_containers?.Clear();
            m_endpoints?.Clear();
            m_files?.Clear();
            m_pipelines?.Clear();
            m_queues?.Clear();
            m_pools?.Clear();
            m_repositories?.Clear();
            m_variableGroups?.Clear();
            m_environments?.Clear();
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_builds?.Count == 0)
            {
                m_builds = null;
            }

            if (m_containers?.Count == 0)
            {
                m_containers = null;
            }

            if (m_endpoints?.Count == 0)
            {
                m_endpoints = null;
            }

            if (m_files?.Count == 0)
            {
                m_files = null;
            }

            if (m_pipelines?.Count == 0)
            {
                m_pipelines = null;
            }

            if (m_queues?.Count == 0)
            {
                m_queues = null;
            }

            if (m_pools?.Count == 0)
            {
                m_pools = null;
            }

            if (m_repositories?.Count == 0)
            {
                m_repositories = null;
            }

            if (m_variableGroups?.Count == 0)
            {
                m_variableGroups = null;
            }

            if (m_environments?.Count == 0)
            {
                m_environments = null;
            }
        }

        [DataMember(Name = "Builds", EmitDefaultValue = false)]
        private HashSet<BuildResource> m_builds;

        [DataMember(Name = "Containers", EmitDefaultValue = false)]
        private HashSet<ContainerResource> m_containers;

        [DataMember(Name = "Endpoints", EmitDefaultValue = false)]
        private HashSet<ServiceEndpointReference> m_endpoints;

        [DataMember(Name = "Files", EmitDefaultValue = false)]
        private HashSet<SecureFileReference> m_files;

        [DataMember(Name = "Pipelines", EmitDefaultValue = false)]
        private HashSet<PipelineResource> m_pipelines;

        [DataMember(Name = "Queues", EmitDefaultValue = false)]
        private HashSet<AgentQueueReference> m_queues;

        [DataMember(Name = "Pools", EmitDefaultValue = false)]
        private HashSet<AgentPoolReference> m_pools;

        [DataMember(Name = "Repositories", EmitDefaultValue = false)]
        private HashSet<RepositoryResource> m_repositories;

        [DataMember(Name = "VariableGroups", EmitDefaultValue = false)]
        private HashSet<VariableGroupReference> m_variableGroups;

        [DataMember(Name = "Environments", EmitDefaultValue = false)]
        private HashSet<EnvironmentReference> m_environments;

        internal abstract class ResourceReferenceComparer<TId, TResource> : IEqualityComparer<TResource> where TResource : ResourceReference
        {
            protected ResourceReferenceComparer(IEqualityComparer<TId> idComparer)
            {
                m_idComparer = idComparer;
            }

            public abstract TId GetId(TResource resource);

            public Boolean Equals(
                TResource left,
                TResource right)
            {
                if (left == null && right == null)
                {
                    return true;
                }

                if ((left != null && right == null) || (left == null && right != null))
                {
                    return false;
                }

                var leftId = GetId(left);
                var rightId = GetId(right);
                if (m_idComparer.Equals(leftId, default(TId)) && m_idComparer.Equals(rightId, default(TId)))
                {
                    return StringComparer.OrdinalIgnoreCase.Equals(left.Name, right.Name);
                }
                else
                {
                    return m_idComparer.Equals(leftId, rightId);
                }
            }

            public Int32 GetHashCode(TResource obj)
            {
                var identifier = GetId(obj);
                if (!m_idComparer.Equals(identifier, default(TId)))
                {
                    return identifier.GetHashCode();
                }
                else
                {
                    return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
                }
            }

            private readonly IEqualityComparer<TId> m_idComparer;
        }

        internal class EndpointComparer : ResourceReferenceComparer<Guid, ServiceEndpointReference>
        {
            public EndpointComparer()
                : base(EqualityComparer<Guid>.Default)
            {
            }

            public override Guid GetId(ServiceEndpointReference resource)
            {
                return resource.Id;
            }
        }

        private class FileComparer : ResourceReferenceComparer<Guid, SecureFileReference>
        {
            public FileComparer()
                : base(EqualityComparer<Guid>.Default)
            {
            }

            public override Guid GetId(SecureFileReference resource)
            {
                return resource.Id;
            }
        }

        private class QueueComparer : ResourceReferenceComparer<Int32, AgentQueueReference>
        {
            public QueueComparer()
                : base(EqualityComparer<Int32>.Default)
            {
            }

            public override Int32 GetId(AgentQueueReference resource)
            {
                return resource.Id;
            }
        }

        private class PoolComparer : ResourceReferenceComparer<Int32, AgentPoolReference>
        {
            public PoolComparer()
                : base(EqualityComparer<Int32>.Default)
            {
            }

            public override Int32 GetId(AgentPoolReference resource)
            {
                return resource.Id;
            }
        }

        private class VariableGroupComparer : ResourceReferenceComparer<Int32, VariableGroupReference>
        {
            public VariableGroupComparer()
                : base(EqualityComparer<Int32>.Default)
            {
            }

            public override Int32 GetId(VariableGroupReference resource)
            {
                return resource.Id;
            }
        }

        private class EnvironmentComparer : ResourceReferenceComparer<Int32, EnvironmentReference>
        {
            public EnvironmentComparer()
                : base(EqualityComparer<Int32>.Default)
            {
            }

            public override Int32 GetId(EnvironmentReference resource)
            {
                return resource.Id;
            }
        }
    }
}
