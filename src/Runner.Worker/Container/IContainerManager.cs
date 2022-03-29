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
        Task<int> ContainerBuild(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag);
        Task<int> ContainerRun(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived);
        Task<int> ContainerExec(string workingDirectory, string fileName, string arguments, string fullPath, IDictionary<string, string> environment, ContainerInfo container, bool requireExitCodeZero, EventHandler<ProcessDataReceivedEventArgs> outputDataReceived, EventHandler<ProcessDataReceivedEventArgs> errorDataReceived, Encoding outputEncoding, bool killProcessOnCancel, object redirectStandardIn, bool inheritConsoleHandler, CancellationToken cancellationToken);
        Task<int> ContainerExec(IExecutionContext context, string containerId, string options, string command, List<string> outputs);
        Task ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container);
        Task<string> CreateContainerNetworkAsync(IExecutionContext executionContext);
        Task StartContainersAsync(IExecutionContext executionContext, List<ContainerInfo> containers);
        Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network);
        Task ContainerCleanup(IExecutionContext executionContext);
        Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container);
        Task StartContainerAsync(IExecutionContext executionContext, ContainerInfo container);
        Task<DictionaryContextData> GetServiceInfo(IExecutionContext executionContext, ContainerInfo container);
        Task GetJobContainerInfo(IExecutionContext executionContext, ContainerInfo container);
        Task<int> EnsureImageExists(IExecutionContext executionContext, string container, string configLocation);
        Task<int> EnsureImageExists(IExecutionContext executionContext, string container);
        string GenerateTag();
    }
}