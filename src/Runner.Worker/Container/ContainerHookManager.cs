using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Container
{
    public class ContainerHookManager : RunnerService, IContainerManager
    {
        public string DockerPath => throw new NotImplementedException();

        public string DockerInstanceLabel => throw new NotImplementedException();

        public Task<DockerVersion> DockerVersion(IExecutionContext context)
        {
            throw new NotImplementedException();
        }
        public Task<int> EnsureImageExists(IExecutionContext context, string image)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<int> EnsureImageExists(IExecutionContext context, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerPull(IExecutionContext context, string image, string configFileDirectory)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerBuild(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag)
        {
            throw new NotImplementedException();
        }

        public Task<string> DockerCreate(IExecutionContext context, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerRun(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerStart(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerLogs(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> DockerPS(IExecutionContext context, string options)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerRemove(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerNetworkCreate(IExecutionContext context, string network)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerNetworkRemove(IExecutionContext context, string network)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerNetworkPrune(IExecutionContext context)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerExec(IExecutionContext context, string containerId, string options, string command, List<string> outputs)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> DockerInspect(IExecutionContext context, string dockerObject, string options)
        {
            throw new NotImplementedException();
        }

        public Task<List<PortMapping>> DockerPort(IExecutionContext context, string containerId)
        {
            throw new NotImplementedException();
        }

        public Task<int> DockerLogin(IExecutionContext context, string configFileDirectory, string registry, string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> ContainersCreate(IExecutionContext context, List<ContainerInfo> containers)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> Container(IExecutionContext context, List<ContainerInfo> containers)
        {
            throw new NotImplementedException();
        }

        Task IContainerManager.ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateContainerNetworkAsync(IExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        public Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network)
        {
            throw new NotImplementedException();
        }

        public Task ContainerCleanup(IExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        public Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task StartContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<DictionaryContextData> GetServiceInfo(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task GetJobContainerInfo(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task StartContainersAsync(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerRun(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            throw new NotImplementedException();
        }

        public Task<int> EnsureImageExists(IExecutionContext executionContext, string container, string configLocation = "")
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerBuild(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext)
        {
            throw new NotImplementedException();
        }

        public string GenerateTag()
        {
            throw new NotImplementedException();
        }
    }
}
