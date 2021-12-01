using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Container
{
    [ServiceLocator(Default = typeof(DockerCommandManager))]
    public interface IContainerCommandManager : IRunnerService
    {
        string Type { get; }
        string DockerPath { get; }
        string DockerInstanceLabel { get; }
        Task<DockerVersion> DockerVersion(IExecutionContext context);
        Task<int> DockerPull(IExecutionContext context, string image);
        Task<int> DockerPull(IExecutionContext context, string image, string configFileDirectory);
        Task<int> DockerBuild(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag);
        Task<string> DockerCreate(IExecutionContext context, ContainerInfo container);
        Task<int> DockerRun(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived);
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
    }
}
