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
    [ServiceLocator(Default = typeof(DockerContainerManager))]
    public interface IContainerManager : IRunnerService
    {
        Task ContainerCleanupAsync(IExecutionContext executionContext);
        Task<string> CreateContainerNetworkAsync(IExecutionContext executionContext);
        Task<int> EnsureImageExistsAsync(IExecutionContext executionContext, string container, string configLocation);
        Task<int> EnsureImageExistsAsync(IExecutionContext executionContext, string container);
        Task<int> ContainerBuildAsync(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag);
        Task StartContainersAsync(IExecutionContext executionContext, List<ContainerInfo> containers);
        Task StartContainerAsync(IExecutionContext executionContext, ContainerInfo container);
        Task GetJobContainerInfo(IExecutionContext executionContext, ContainerInfo container);
        Task<DictionaryContextData> GetServiceInfoAsync(IExecutionContext executionContext, ContainerInfo container);
        Task ContainerHealthcheckAsync(IExecutionContext executionContext, ContainerInfo container);
        Task<int> ContainerRunAsync(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived);
        Task<int> ContainerExecAsync(string workingDirectory, string fileName, string arguments, string fullPath, IDictionary<string, string> environment, ContainerInfo container, bool requireExitCodeZero, EventHandler<ProcessDataReceivedEventArgs> outputDataReceived, EventHandler<ProcessDataReceivedEventArgs> errorDataReceived, Encoding outputEncoding, bool killProcessOnCancel, object redirectStandardIn, bool inheritConsoleHandler, CancellationToken cancellationToken);
        Task<int> ContainerExecAsync(IExecutionContext context, string containerId, string options, string command, List<string> outputs);
        Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network);
        Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container);
        string GenerateContainerTag();
    }
}