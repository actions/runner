using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Artifacts;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Artifacts;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Provides a default implementation of a resource store.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ResourceStore : IResourceStore
    {
        /// <summary>
        /// Initializes a new <c>ResourceStore</c> instance with no resources.
        /// </summary>
        public ResourceStore()
            : this(endpoints: null)
        {
        }

        /// <summary>
        /// Initializes a new <c>ResourceStore</c> instance with the specified resources. If aliases are provided, 
        /// an alias overrides lookup by name for the specified resource.
        /// </summary>
        /// <param name="endpoints">The collection of endpoints available in the store</param>
        /// <param name="files">The collection of secure files available in the store</param>
        /// <param name="queues">The collection of agent queues available in the store</param>
        /// <param name="variableGroups">The collection of variable groups available in the store</param>
        public ResourceStore(
            IList<ServiceEndpoint> endpoints = null,
            IList<SecureFile> files = null,
            IList<TaskAgentQueue> queues = null,
            IList<VariableGroup> variableGroups = null,
            IList<BuildResource> builds = null,
            IList<ContainerResource> containers = null,
            IList<RepositoryResource> repositories = null,
            IList<PipelineResource> pipelines = null,
            IList<TaskAgentPool> pools = null)
            : this(new ServiceEndpointStore(endpoints), new SecureFileStore(files), new AgentQueueStore(queues), new VariableGroupStore(variableGroups), new BuildResourceStore(builds), new ContainerResourceStore(containers), new RepositoryResourceStore(repositories), new PipelineResourceStore(pipelines), new AgentPoolStore(pools), new EnvironmentStore(null))
        {
        }

        /// <summary>
        /// Initializes a new <c>ResourceStore</c> instance with the specified resources and endpoint store. If 
        /// aliases are provided, an alias overrides lookup by name for the specified resource.
        /// </summary>
        /// <param name="endpointStore">The store for retrieving referenced service endpoints</param>
        /// <param name="fileStore">The store for retrieving referenced secure files</param>
        /// <param name="queueStore">The store for retrieving referenced agent queues</param>
        /// <param name="variableGroupStore">The store for retrieving reference variable groups</param>
        public ResourceStore(
            IServiceEndpointStore endpointStore = null,
            ISecureFileStore fileStore = null,
            IAgentQueueStore queueStore = null,
            IVariableGroupStore variableGroupStore = null,
            IBuildStore buildStore = null,
            IContainerStore containerStore = null,
            IRepositoryStore repositoryStore = null,
            IPipelineStore pipelineStore = null,
            IAgentPoolStore poolStore = null,
            IEnvironmentStore environmentStore = null)
        {
            this.Builds = buildStore ?? new BuildResourceStore(null);
            this.Containers = containerStore ?? new ContainerResourceStore(null);
            this.Endpoints = endpointStore ?? new ServiceEndpointStore(null);
            this.Files = fileStore ?? new SecureFileStore(null);
            this.Pipelines = pipelineStore ?? new PipelineResourceStore(null);
            this.Queues = queueStore ?? new AgentQueueStore(null);
            this.Pools = poolStore ?? new AgentPoolStore(null);
            this.Repositories = repositoryStore ?? new RepositoryResourceStore(null);
            this.VariableGroups = variableGroupStore ?? new VariableGroupStore(null);
            this.Environments = environmentStore ?? new EnvironmentStore(null);
        }

        /// <summary>
        /// Gets the store used for retrieving build resources.
        /// </summary>
        public IBuildStore Builds
        {
            get;
        }

        /// <summary>
        /// Gets the store used for retrieving container resources.
        /// </summary>
        public IContainerStore Containers
        {
            get;
        }

        /// <summary>
        /// Gets the store used for retrieving service endpoints.
        /// </summary>
        public IServiceEndpointStore Endpoints
        {
            get;
        }

        /// <summary>
        /// Gets the store used for retrieving environment.
        /// </summary>
        public IEnvironmentStore Environments
        {
            get;
        }

        /// <summary>
        /// Gets the store used for retrieving secure files.
        /// </summary>
        public ISecureFileStore Files
        {
            get;
        }

        /// <summary>
        /// Get the store used for retrieving pipelines.
        /// </summary>
        public IPipelineStore Pipelines
        {
            get;
        }

        /// <summary>
        /// Gets the store used for retrieving agent queues.
        /// </summary>
        public IAgentQueueStore Queues
        {
            get;
        }

        /// <summary>
        /// Gets the store used for retrieving agent pools.
        /// </summary>
        public IAgentPoolStore Pools
        {
            get;
        }

        /// <summary>
        /// Gets the store used for retrieving repository resources.
        /// </summary>
        public IRepositoryStore Repositories
        {
            get;
        }

        /// <summary>
        /// Gets the store used for retrieving variable groups.
        /// </summary>
        public IVariableGroupStore VariableGroups
        {
            get;
        }

        /// <summary>
        /// Gets all resources currently in the resource store.
        /// </summary>
        /// <returns></returns>
        public PipelineResources GetAuthorizedResources()
        {
            var resources = new PipelineResources();
            resources.Builds.AddRange(this.Builds.GetAll());
            resources.Containers.AddRange(this.Containers.GetAll());
            resources.Endpoints.AddRange(this.Endpoints.GetAuthorizedReferences());
            resources.Files.AddRange(this.Files.GetAuthorizedReferences());
            resources.Pipelines.AddRange(this.Pipelines.GetAll());
            resources.Queues.AddRange(this.Queues.GetAuthorizedReferences());
            resources.Pools.AddRange(this.Pools.GetAuthorizedReferences());
            resources.Repositories.AddRange(this.Repositories.GetAll());
            resources.VariableGroups.AddRange(this.VariableGroups.GetAuthorizedReferences());
            resources.Environments.AddRange(this.Environments.GetReferences());
            return resources;
        }

        /// <summary>
        /// Gets the steps, if any, which should be inserted into the job based on the resources configured.
        /// </summary>
        /// <param name="context">The execution context</param>
        /// <param name="steps">The current set of steps for the job</param>
        /// <returns>A list of steps which should be prepended to the job</returns>
        public IList<TaskStep> GetPreSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            var allSteps = new List<TaskStep>();
            if (context.EnvironmentVersion > 1 && context is PipelineExecutionContext)
            {
                // Variable group steps are always set first in case the other steps depend on the values
                allSteps.AddRangeIfRangeNotNull(this.VariableGroups.GetPreSteps(context, steps));

                // Now just do the remaining resources in alphabetical order
                allSteps.AddRangeIfRangeNotNull(this.Builds.GetPreSteps(context, steps));
                allSteps.AddRangeIfRangeNotNull(this.Repositories.GetPreSteps(context, steps));
                allSteps.AddRangeIfRangeNotNull(this.Pipelines.GetPreSteps(context, steps));
            }

            return allSteps;
        }

        /// <summary>
        /// Get steps that are run after all other steps.
        /// </summary>
        /// <param name="context">The execution context</param>
        /// <returns></returns>
        public IList<TaskStep> GetPostSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            return new List<TaskStep>();
        }

        public ServiceEndpoint GetEndpoint(Guid endpointId)
        {
            return this.Endpoints.Get(new ServiceEndpointReference { Id = endpointId });
        }

        public ServiceEndpoint GetEndpoint(String endpointId)
        {
            ServiceEndpoint endpoint = null;
            if (Guid.TryParse(endpointId, out Guid endpointIdValue))
            {
                endpoint = GetEndpoint(endpointIdValue);
            }

            if (endpoint == null)
            {
                endpoint = this.Endpoints.Get(new ServiceEndpointReference { Name = endpointId });
            }

            return endpoint;
        }

        public SecureFile GetFile(Guid fileId)
        {
            return this.Files.Get(new SecureFileReference { Id = fileId });
        }

        public SecureFile GetFile(String fileId)
        {
            SecureFile file = null;
            if (Guid.TryParse(fileId, out Guid fileIdValue))
            {
                file = GetFile(fileIdValue);
            }

            if (file == null)
            {
                file = this.Files.Get(new SecureFileReference { Name = fileId });
            }

            return file;
        }

        public TaskAgentQueue GetQueue(Int32 queueId)
        {
            return this.Queues.Get(new AgentQueueReference { Id = queueId });
        }

        public TaskAgentQueue GetQueue(String queueId)
        {
            TaskAgentQueue queue = null;
            if (Int32.TryParse(queueId, out Int32 queueIdValue))
            {
                queue = GetQueue(queueIdValue);
            }

            if (queue == null)
            {
                queue = this.Queues.Get(new AgentQueueReference { Name = queueId });
            }

            return queue;
        }

        public TaskAgentPool GetPool(Int32 poolId)
        {
            return this.Pools.Get(new AgentPoolReference { Id = poolId });
        }

        public TaskAgentPool GetPool(String poolName)
        {
            return this.Pools.Get(new AgentPoolReference { Name = poolName });
        }

        public VariableGroup GetVariableGroup(Int32 groupId)
        {
            return this.VariableGroups.Get(new VariableGroupReference { Id = groupId });
        }

        public VariableGroup GetVariableGroup(String groupId)
        {
            VariableGroup variableGroup = null;
            if (Int32.TryParse(groupId, out Int32 groupIdValue))
            {
                variableGroup = GetVariableGroup(groupIdValue);
            }

            if (variableGroup == null)
            {
                variableGroup = this.VariableGroups.Get(new VariableGroupReference { Name = groupId });
            }

            return variableGroup;
        }

        public Boolean ResolveStep(
            IPipelineContext context,
            JobStep step,
            out IList<TaskStep> resolvedSteps)
        {
            resolvedSteps = new List<TaskStep>();
            if (context.EnvironmentVersion > 1 && context is PipelineExecutionContext)
            {
                return this.Pipelines.ResolveStep(context, step, out resolvedSteps);
            }

            return false;
        }
    }

    public abstract class InMemoryResourceStore<T> where T : Resource
    {
        protected InMemoryResourceStore(IEnumerable<T> resources)
        {
            m_resources = resources?.ToDictionary(x => x.Alias, x => x, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<String, T>(StringComparer.OrdinalIgnoreCase);
        }

        public Int32 Count => m_resources.Count;

        public void Add(T resource)
        {
            m_resources.Add(resource.Alias, resource);
        }

        public void Add(IEnumerable<T> resources)
        {
            foreach (var resource in resources)
            {
                m_resources.Add(resource.Alias, resource);
            }
        }

        public T Get(String alias)
        {
            if (m_resources.TryGetValue(alias, out T resource))
            {
                return resource;
            }

            return null;
        }

        public IEnumerable<T> GetAll()
        {
            return m_resources.Values.ToList();
        }

        private Dictionary<String, T> m_resources;
    }

    public class BuildResourceStore : InMemoryResourceStore<BuildResource>, IBuildStore
    {
        public BuildResourceStore(IEnumerable<BuildResource> builds)
            : base(builds)
        {
        }

        public BuildResourceStore(params BuildResource[] builds)
            : base(builds)
        {
        }

        public IList<TaskStep> GetPreSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            return null;
        }

        public IList<TaskStep> GetPostSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            return new List<TaskStep>();
        }

        public Boolean ResolveStep(
            IPipelineContext context,
            JobStep step,
            out IList<TaskStep> resolvedSteps)
        {
            resolvedSteps = new List<TaskStep>();
            return false;
        }
    }

    public class ContainerResourceStore : InMemoryResourceStore<ContainerResource>, IContainerStore
    {
        public ContainerResourceStore(IEnumerable<ContainerResource> containers)
            : base(containers)
        {
        }

        public ContainerResourceStore(params ContainerResource[] containers)
            : base(containers)
        {
        }

        public bool ResolveStep(
            IPipelineContext context,
            JobStep step,
            out IList<TaskStep> resolvedSteps)
        {
            resolvedSteps = new List<TaskStep>();
            return false;
        }
    }

    public class PipelineResourceStore : InMemoryResourceStore<PipelineResource>, IPipelineStore
    {
        public PipelineResourceStore(
            IEnumerable<PipelineResource> pipelines,
            IArtifactResolver artifactResolver = null,
            Boolean isEnabled = false,
            Boolean useSystemStepsDecorator = false)
            : base(pipelines)
        {
            this.m_artifactResolver = artifactResolver;
            this.m_isEnabled = isEnabled;
            this.m_useSystemStepsDecorator = useSystemStepsDecorator;
        }

        public IList<TaskStep> GetPreSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            return new List<TaskStep>();
        }

        public IList<TaskStep> GetPostSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            return new List<TaskStep>();
        }

        public Boolean ResolveStep(
            IPipelineContext context,
            JobStep step,
            out IList<TaskStep> resolvedSteps)
        {
            resolvedSteps = new List<TaskStep>();

            if (step.IsDownloadTask())
            {
                if (!m_isEnabled)
                {
                    // The pre step decorator can't check the FF state. It always adds a download step for a current pipeline.
                    // To make sure we aren't failing all the existing pipelines, if the DownloadStep FF is not enabled we will return as resolved with empty resolved steps.
                    return true;
                }

                return m_artifactResolver?.ResolveStep(context, step, out resolvedSteps) ?? false;
            }

            return false;
        }

        private IArtifactResolver m_artifactResolver;
        private Boolean m_isEnabled;
        private Boolean m_useSystemStepsDecorator;
    }

    public class RepositoryResourceStore : InMemoryResourceStore<RepositoryResource>, IRepositoryStore
    {
        public RepositoryResourceStore(IEnumerable<RepositoryResource> repositories)
            : this(repositories, false, false)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public RepositoryResourceStore(
            IEnumerable<RepositoryResource> repositories,
            Boolean useSystemStepsDecorator,
            Boolean includeCheckoutOptions)
            : base(repositories)
        {
            m_useSystemStepsDecorator = useSystemStepsDecorator;
            m_includeCheckoutOptions = includeCheckoutOptions;
        }

        public IList<TaskStep> GetPreSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            // If the environment version is 1 we should not inject
            if (context.EnvironmentVersion < 2)
            {
                return null;
            }

            var executionContext = context as JobExecutionContext;
            if (context is JobExecutionContext jobContext && (jobContext.Phase.Definition as Phase)?.Target.Type != PhaseTargetType.Queue)
            {
                // only inject checkout step for agent phase
                return null;
            }

            // Check feature flag DistributedTask.IncludeCheckoutOptions.
            // Controls whether the checkout options are merged into the task inputs,
            // or whether the checkout task does.
            if (!m_includeCheckoutOptions)
            {
                // Populate default checkout option from repository into task's inputs
                foreach (var checkoutTask in steps.Where(x => x.IsCheckoutTask()).OfType<TaskStep>())
                {
                    var repository = Get(checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.Repository]);
                    if (repository != null && repository.Properties.TryGetValue<CheckoutOptions>(RepositoryPropertyNames.CheckoutOptions, out CheckoutOptions checkoutOptions))
                    {
                        MergeCheckoutOptions(checkoutOptions, checkoutTask);
                    }
                }
            }

            // Check feature flag DistributedTask.YamlSystemStepsDecorator.
            // Controls whether to load the checkout step from a YAML template.
            if (m_useSystemStepsDecorator)
            {
                return null;
            }

            var selfRepo = Get(PipelineConstants.SelfAlias);
            if (selfRepo == null)
            {
                // self repository doesn't existing, no needs to inject checkout task.
                // self repo is for yaml only, designer build should always provide checkout task
                return null;
            }
            else
            {
                // If any steps contains checkout task, we will not inject checkout task
                if (steps.Any(x => x.IsCheckoutTask()))
                {
                    return null;
                }
                else
                {
                    //Inject checkout:self task
                    var checkoutTask = new TaskStep()
                    {
                        Enabled = true,
                        DisplayName = PipelineConstants.CheckoutTask.FriendlyName,
                        Reference = new TaskStepDefinitionReference()
                        {
                            Id = PipelineConstants.CheckoutTask.Id,
                            Version = PipelineConstants.CheckoutTask.Version,
                            Name = PipelineConstants.CheckoutTask.Name
                        }
                    };

                    checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.Repository] = selfRepo.Alias;
                    if (selfRepo.Properties.TryGetValue(RepositoryPropertyNames.CheckoutOptions, out CheckoutOptions checkoutOptions))
                    {
                        MergeCheckoutOptions(checkoutOptions, checkoutTask);
                    }

                    return new[] { checkoutTask };
                }
            }
        }

        public IList<TaskStep> GetPostSteps(
            IPipelineContext context,
            IReadOnlyList<JobStep> steps)
        {
            return new List<TaskStep>();
        }

        private void MergeCheckoutOptions(
            CheckoutOptions checkoutOptions,
            TaskStep checkoutTask)
        {
            if (!checkoutTask.Inputs.ContainsKey(PipelineConstants.CheckoutTaskInputs.Clean) && !String.IsNullOrEmpty(checkoutOptions.Clean))
            {
                checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.Clean] = checkoutOptions.Clean;
            }

            if (!checkoutTask.Inputs.ContainsKey(PipelineConstants.CheckoutTaskInputs.FetchDepth) && !String.IsNullOrEmpty(checkoutOptions.FetchDepth))
            {
                checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.FetchDepth] = checkoutOptions.FetchDepth;
            }

            if (!checkoutTask.Inputs.ContainsKey(PipelineConstants.CheckoutTaskInputs.Lfs) && !String.IsNullOrEmpty(checkoutOptions.Lfs))
            {
                checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.Lfs] = checkoutOptions.Lfs;
            }

            if (!checkoutTask.Inputs.ContainsKey(PipelineConstants.CheckoutTaskInputs.PersistCredentials) && !String.IsNullOrEmpty(checkoutOptions.PersistCredentials))
            {
                checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.PersistCredentials] = checkoutOptions.PersistCredentials;
            }

            if (!checkoutTask.Inputs.ContainsKey(PipelineConstants.CheckoutTaskInputs.Submodules) && !String.IsNullOrEmpty(checkoutOptions.Submodules))
            {
                checkoutTask.Inputs[PipelineConstants.CheckoutTaskInputs.Submodules] = checkoutOptions.Submodules;
            }
        }

        public Boolean ResolveStep(
            IPipelineContext context,
            JobStep step,
            out IList<TaskStep> resolvedSteps)
        {
            resolvedSteps = new List<TaskStep>();
            return false;
        }

        private Boolean m_useSystemStepsDecorator;
        private Boolean m_includeCheckoutOptions;
    }
}
