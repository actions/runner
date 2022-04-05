using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Handlers;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Worker.Container
{
    [ServiceLocator(Default = typeof(ContainerHookManager))]
    public interface IContainerHookManager : IRunnerService
    {
        Task<int> PrepareJobAsync(IExecutionContext context);
        Task<int> JobCleanupAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task<int> StepContainerAsync(IExecutionContext context);
        Task<int> StepScriptAsync(IExecutionContext context);
    }

    public class ContainerHookManager : RunnerService, IContainerHookManager
    {
        public async Task<int> PrepareJobAsync(IExecutionContext context)
        {
            Trace.Entering();

            var meta = new ContainerHookMeta
            {
                Command = "prepare_job", // TODO: work out   GetHookCommand(nameof(PrepareJobAsync))
                ResponseFile = "response.json",
            };
            // TODO: figure out hook args
            return await ExecuteHookScript(context, GetHookIndexPath(), meta);
        }

        public async Task<int> JobCleanupAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();

            var meta = new ContainerHookMeta
            {
                Command = GetHookCommand(nameof(JobCleanupAsync)),
                ResponseFile = "response.json",
                Args = new ContainerHookArgs
                {
                    Containers = containers.Select(c => new ContainerHookContainer { ContainerId = c.ContainerId, ContainerNetwork = c.ContainerNetwork }).ToList()
                }
            };
            // TODO: figure out hook args
            return await ExecuteHookScript(context, GetHookIndexPath(), meta);
        }

        public async Task<int> StepContainerAsync(IExecutionContext context)
        {
            Trace.Entering();
            await Task.FromResult(0);
            throw new NotImplementedException();
        }

        public async Task<int> StepScriptAsync(IExecutionContext context)
        {
            Trace.Entering();
            await Task.FromResult(0);
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

        private string GetHookIndexPath()
        {
            return Path.Combine(HostContext.GetDirectory(WellKnownDirectory.ContainerHooks), "index.js");
        }

        private static string GetHookCommand(string commandName)
        {
            return commandName.ToLower().Replace("async", "");
        }
    }
}