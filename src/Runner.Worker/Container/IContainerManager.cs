using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Container
{
    [ServiceLocator(Default = typeof(DockerContainerManager))]
    public interface IContainerManager : IRunnerService
    {
        Task ContainerCleanupAsync(IExecutionContext executionContext);
        Task<string> NetworkCreateAsync(IExecutionContext executionContext);
        Task<string> RegistryLoginAsync(IExecutionContext executionContext, ContainerInfo container);
        Task<int> ContainerPullAsync(IExecutionContext executionContext, string container, string configLocation);
        Task<int> ContainerPullAsync(IExecutionContext executionContext, string container);
        void RegistryLogout(string configLocation);
        Task<int> ContainerBuildAsync(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag);
        Task<string> ContainerCreateAsync(IExecutionContext executionContext, ContainerInfo container);
        Task<int> ContainerStartAsync(IExecutionContext executionContext, ContainerInfo container);
        Task ContainerStartAllJobDependencies(IExecutionContext executionContext, List<ContainerInfo> containers);
        Task LogContainerStartupInfo(IExecutionContext executionContext, ContainerInfo container);
        Task<string> ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container);
        Task<string> ContainerGetRuntimePathAsync(IExecutionContext executionContext, ContainerInfo container);
        Task<List<PortMapping>> ContainerPort(IExecutionContext executionContext, ContainerInfo container);
        Task<int> ContainerRunAsync(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived);
        Task<int> ContainerExecAsync(string workingDirectory, string fileName, string arguments, string fullPath, IDictionary<string, string> environment, ContainerInfo container, bool requireExitCodeZero, EventHandler<ProcessDataReceivedEventArgs> outputDataReceived, EventHandler<ProcessDataReceivedEventArgs> errorDataReceived, Encoding outputEncoding, bool killProcessOnCancel, object redirectStandardIn, bool inheritConsoleHandler, CancellationToken cancellationToken);
        Task<int> ContainerExecAsync(IExecutionContext context, string containerId, string options, string command, List<string> outputs);
        Task NetworkRemoveAsync(IExecutionContext executionContext, string network);
        Task NetworkPruneAsync(IExecutionContext executionContext);
        Task ContainerPruneAsync(IExecutionContext executionContext, List<ContainerInfo> containers);
        Task ContainerRemoveAsync(IExecutionContext executionContext, ContainerInfo container);
        string GenerateContainerTag();
        string ContainerManagerName { get; }
    }
}