using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    public interface IBuildStore : IStepProvider
    {
        void Add(BuildResource resource);
        void Add(IEnumerable<BuildResource> resources);
        BuildResource Get(String alias);
        IEnumerable<BuildResource> GetAll();
    }

    public interface IContainerStore
    {
        void Add(ContainerResource resource);
        void Add(IEnumerable<ContainerResource> resources);
        ContainerResource Get(String alias);
        IEnumerable<ContainerResource> GetAll();
    }

    public interface IPipelineStore : IStepProvider
    {
        void Add(PipelineResource resource);
        void Add(IEnumerable<PipelineResource> resources);
        PipelineResource Get(String alias);
        IEnumerable<PipelineResource> GetAll();
    }

    public interface IRepositoryStore : IStepProvider
    {
        void Add(RepositoryResource resource);
        void Add(IEnumerable<RepositoryResource> resources);
        RepositoryResource Get(String alias);
        IEnumerable<RepositoryResource> GetAll();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IResourceStore : IStepProvider
    {
        IBuildStore Builds { get; }

        IContainerStore Containers { get; }

        IServiceEndpointStore Endpoints { get; }

        ISecureFileStore Files { get; }

        IEnvironmentStore Environments { get; }

        IPipelineStore Pipelines { get; }

        IAgentQueueStore Queues { get; }

        IAgentPoolStore Pools { get; }

        IRepositoryStore Repositories { get; }

        IVariableGroupStore VariableGroups { get; }

        PipelineResources GetAuthorizedResources();

        ServiceEndpoint GetEndpoint(Guid endpointId);

        ServiceEndpoint GetEndpoint(String endpointId);

        SecureFile GetFile(Guid fileId);

        SecureFile GetFile(String fileId);

        TaskAgentQueue GetQueue(Int32 queueId);

        TaskAgentQueue GetQueue(String queueId);

        TaskAgentPool GetPool(Int32 poolId);

        TaskAgentPool GetPool(String poolName);

        VariableGroup GetVariableGroup(Int32 groupId);

        VariableGroup GetVariableGroup(String groupId);
    }
}
