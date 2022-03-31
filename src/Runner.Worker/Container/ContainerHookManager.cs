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
    public class ContainerHookManager : RunnerService
    {
        public string ContainerManagerName => "Container Hook";

        public string RegistryConfigFile => throw new NotImplementedException();

        public Task<int> ContainerBuildAsync(IExecutionContext context, string workingDirectory, string dockerFile, string dockerContext, string tag)
        {
            throw new NotImplementedException();
        }

        public Task ContainerCleanupAsync(IExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        public Task<string> ContainerCreateAsync(IExecutionContext executionContext, ContainerInfo container)
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

        public Task<string> ContainerGetRuntimePathAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<string> ContainerHealthcheck(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<List<PortMapping>> ContainerPort(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerPullAsync(IExecutionContext executionContext, string container, string configLocation)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerPullAsync(IExecutionContext executionContext, string container)
        {
            throw new NotImplementedException();
        }

        public Task<int> RegistryLoginAsync(IExecutionContext executionContext, string configLocation, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public void RegistryLogout(string configLocation)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerRunAsync(IExecutionContext context, ContainerInfo container, EventHandler<ProcessDataReceivedEventArgs> stdoutDataReceived, EventHandler<ProcessDataReceivedEventArgs> stderrDataReceived)
        {
            throw new NotImplementedException();
        }

        public Task ContainerStartAllJobDependencies(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            throw new NotImplementedException();
        }

        public Task<int> ContainerStartAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task ContainerPruneAsync(IExecutionContext executionContext, List<ContainerInfo> containers)
        {
            throw new NotImplementedException();
        }

        public Task ContainerRemoveAsync(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public string GenerateContainerTag()
        {
            throw new NotImplementedException();
        }

        public Task LogContainerStartupInfo(IExecutionContext executionContext, ContainerInfo container)
        {
            throw new NotImplementedException();
        }

        public Task<string> NetworkCreateAsync(IExecutionContext executionContext)
        {
            throw new NotImplementedException();
        }

        public Task NetworkRemoveAsync(IExecutionContext executionContext, string network)
        {
            throw new NotImplementedException();
        }

        // public async Task ContainerCleanupAsync(IExecutionContext executionContext)
        // {
        //     const string HookName = nameof(ContainerCleanupAsync);
        //     if (!IsContainerHookActive(HookName, out var hookScriptPath))
        //     {
        //         throw new Exception($"{HookName} is not supported");
        //     }
        //     var containerHookArgs = new ContainerHookMeta
        //     {
        //         Command = HookName,
        //         ResponseFile = "",
        //         Args = new ContainerHookArgs
        //         {
        //             Container = new ContainerHookContainer
        //             {
        //                 ContainerId = "foo",
        //                 Network = "bar"
        //             }
        //         }
        //     };
        //     await ExecuteHookScript(executionContext, hookScriptPath, containerHookArgs);
        // }


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
    }
}