using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Container
{

    // this class should execute the hooks, prepare their inputs and handle their outputs
    public class ContainerHookManager : RunnerService, IContainerManager
    {
        public Task<int> ContainerBuildAsync(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag)
        {
            throw new NotImplementedException();
        }

        public Task ContainerCleanupAsync(IExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerExecAsync(string workingDirectory, string fileName, string arguments, string fullPath, IDictionary<string, string> environment, ContainerInfo container, bool requireExitCodeZero, EventHandler<ProcessDataReceivedEventArgs> outputDataReceived, EventHandler<ProcessDataReceivedEventArgs> errorDataReceived, Encoding outputEncoding, bool killProcessOnCancel, object redirectStandardIn, bool inheritConsoleHandler, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerExecAsync(IExecutionContext context, string containerId, string options, string command, List<string> outputs)
        {
            throw new NotImplementedException();
        }

        public Task ContainerHealthcheckAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerRunAsync(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreateContainerNetworkAsync(IExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        public Task<int> EnsureImageExistsAsync(IExecutionContext executionContext, string container, string configLocation)
        {
            throw new NotImplementedException();
        }

        public Task<int> EnsureImageExistsAsync(IExecutionContext executionContext, string container)
        {
            throw new NotImplementedException();
        }

        public string GenerateContainerTag()
        {
            throw new NotImplementedException();
        }

        public Task GetJobContainerInfo(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<DictionaryContextData> GetServiceInfoAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network)
        {
            throw new NotImplementedException();
        }

        public Task StartContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task StartContainersAsync(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            throw new NotImplementedException();
        }

        public Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }
    }
}