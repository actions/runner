using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Handlers;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Worker.Container
{
    // this class should execute the hooks, prepare their inputs and handle their outputs
    public class ContainerHookManager : RunnerService, IContainerManager
    {

        public async Task ContainerCleanupAsync(IExecutionContext executionContext)
        {
            const string HookName = nameof(ContainerCleanupAsync);
            if (!IsContainerHookActive(HookName, out var hookScriptPath))
            {
                throw new Exception($"{HookName} is not supported");
            }
            var containerHookArgs = new ContainerHookMeta
            {
                Command = HookName,
                ResponseFile = "",
                Args = new ContainerHookArgs
                {
                    Container = new ContainerHookContainer
                    {
                        ContainerId = "foo",
                        Network = "bar"
                    }
                }
            };
            await ExecuteHookScript(executionContext, hookScriptPath, containerHookArgs);
        }

        public async Task<string> CreateContainerNetworkAsync(IExecutionContext executionContext)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task<int> EnsureImageExistsAsync(IExecutionContext executionContext, string container, string configLocation)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task<int> EnsureImageExistsAsync(IExecutionContext executionContext, string container)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task<int> ContainerBuildAsync(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task StartContainersAsync(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task StartContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task GetJobContainerInfo(IExecutionContext executionContext, ContainerInfo container)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task<DictionaryContextData> GetServiceInfoAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task ContainerHealthcheckAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task<int> ContainerRunAsync(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task<int> ContainerExecAsync(string workingDirectory, string fileName, string arguments, string fullPath, IDictionary<string, string> environment, ContainerInfo container, bool requireExitCodeZero, EventHandler<ProcessDataReceivedEventArgs> outputDataReceived, EventHandler<ProcessDataReceivedEventArgs> errorDataReceived, Encoding outputEncoding, bool killProcessOnCancel, object redirectStandardIn, bool inheritConsoleHandler, CancellationToken cancellationToken)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task<int> ContainerExecAsync(IExecutionContext context, string containerId, string options, string command, List<string> outputs)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task RemoveContainerNetworkAsync(IExecutionContext executionContext, string network)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public async Task StopContainerAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            await Task.FromResult(0); throw new NotImplementedException();
        }

        public string GenerateContainerTag()
        {
            throw new NotImplementedException();
        }

        private async Task<int> ExecuteHookScript(IExecutionContext context, string hookScriptPath, ContainerHookMeta args)
        {
            var scriptDirectory = Path.GetDirectoryName(hookScriptPath);
            var stepHost = HostContext.CreateService<IDefaultStepHost>();
            Dictionary<string, string> inputs = new()
            {
                ["standardInInput"] = JsonUtility.ToString(args),
                ["path"] = hookScriptPath,
                ["shell"] = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), NodeUtil.GetInternalNodeVersion(), "bin", $"node{IOUtil.ExeExtension}") + " {0}" // TODO: fix hardcoded node path
            };

            var handlerFactory = HostContext.GetService<IHandlerFactory>();
            var handler = handlerFactory.Create(
                            context,
                            null,
                            stepHost,
                            new ScriptActionExecutionData(),
                            inputs,
                            environment: new Dictionary<string, string>(VarUtil.EnvironmentVariableKeyComparer),
                            context.Global.Variables,
                            actionDirectory: scriptDirectory,
                            localActionContainerSetupSteps: null) as ScriptHandler;
            handler.PrepareExecution(ActionRunStage.Pre); // TODO: find out stage, we only use Start in pre, but double check
            await handler.RunAsync(ActionRunStage.Pre);
            return handler.ExecutionContext.CommandResult == TaskResult.Succeeded ? 0 : 1;
        }
        private bool IsContainerHookActive(string hookName, out string path)
        {
            // TODO: do we want this flag to come from the .yml as well?
            var areDockerHooksAllowed = StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_ALLOW_DOCKER_OVERRIDE"));
            path = GetContainerHookFilePath(hookName);
            return areDockerHooksAllowed && File.Exists(path);
        }

        private string GetContainerHookFilePath(string commandName)
        {
            commandName = string.Format("{0}.js", commandName.ToLower());
            return Path.Combine(HostContext.GetDirectory(WellKnownDirectory.DockerHooks), commandName);
        }
    }
}