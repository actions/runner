using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Container
{
    [ServiceLocator(Default = typeof(DockerContainerManager))]
    public interface IContainerManager : IRunnerService
    {
        string DockerPath { get; }
        string DockerInstanceLabel { get; }
        Task<DockerVersion> DockerVersion(IExecutionContext context);
        Task<int> DockerPull(IExecutionContext context, string image, string configFileDirectory);
        Task<int> DockerBuild(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag);
        Task<string> DockerCreate(IExecutionContext context, ContainerInfo container);
        Task<int> ContainerRun(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived);
        Task<int> DockerStart(IExecutionContext context, string containerId);
        Task<int> DockerLogs(IExecutionContext context, string containerId);
        Task<List<string>> DockerPS(IExecutionContext context, string options);
        Task<int> DockerRemove(IExecutionContext context, string containerId);
        Task<int> DockerNetworkCreate(IExecutionContext context, string network);
        Task<int> DockerNetworkRemove(IExecutionContext context, string network);
        Task<int> DockerNetworkPrune(IExecutionContext context);
        Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command);
        Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command, List<string> outputs);
        Task<List<string>> DockerInspect(IExecutionContext context, string dockerObject, string options);
        Task<List<PortMapping>> DockerPort(IExecutionContext context, string containerId);
        Task<int> DockerLogin(IExecutionContext context, string configFileDirectory, string registry, string username, string password);
        Task<List<string>> ContainersCreate(IExecutionContext context, List<ContainerInfo> containers);
        Task<List<string>> Container(IExecutionContext context, List<ContainerInfo> containers);
        Task ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container);
        Task<string> CreateContainerNetworkAsync(IExecutionContext executionContext);
        Task StartContainersAsync(IExecutionContext executionContext, List<ContainerInfo> containers);
        Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network);
        Task ContainerCleanup(IExecutionContext executionContext);
        Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container);
        Task StartContainerAsync(IExecutionContext executionContext, ContainerInfo container);
        Task<DictionaryContextData> GetServiceInfo(IExecutionContext executionContext, ContainerInfo container);
        Task GetJobContainerInfo(IExecutionContext executionContext, ContainerInfo container);
        Task<int> EnsureImageExists(IExecutionContext executionContext, string container, string configLocation = "");
    }
}