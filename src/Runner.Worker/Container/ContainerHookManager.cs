using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    [ServiceLocator(Default = typeof(ContainerHookManager))]
    public interface IContainerHookManager : IRunnerService
    {
        Task<int> PrepareJobAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task<int> JobCleanupAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task<int> StepContainerAsync(IExecutionContext context);
        Task<int> StepScriptAsync(IExecutionContext context);
    }

    public class ContainerHookManager : RunnerService, IContainerHookManager
    {
        public async Task<int> PrepareJobAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();

            var hookIndexPath = HostContext.GetDirectory(WellKnownDirectory.ContainerHooks);
            var responsePath = $"{hookIndexPath}/response.json";
            using (StreamWriter w = File.AppendText(responsePath)){ }

            var meta = new ContainerHookMeta
            {
                Command = "prepare_job", // TODO: work out   GetHookCommand(nameof(PrepareJobAsync))
                ResponseFile = responsePath,
                Args = new ContainerHookArgs
                {
                    JobContainer = containers.Where(c => c.IsJobContainer).FirstOrDefault(),
                    ServiceContainers = containers.Where(c => c.IsJobContainer == false).ToList()
                }
            };

            var exitCode = await ExecuteHookScript(context, GetHookIndexPath(), meta);
            if (exitCode != 0)
            {
                throw new Exception("Hook failed"); // TODO: fail or fallback?
            }

            var response = JsonUtility.FromString<ContainerHookResponse>(await File.ReadAllTextAsync(responsePath));
            File.Delete(responsePath);
            var containerId = response.Context.Container.Id;
            var containerNetwork = response.Context.Container.Network;

            context.JobContext.Container["id"] = new StringContextData(containerId);
            context.JobContext.Container["network"] = new StringContextData(containerNetwork);
            var jc = containers.Where(c => c.IsJobContainer).FirstOrDefault();
            jc.ContainerId = containerId;
            jc.ContainerNetwork = containerNetwork;
            // context.JobContext["state"] = new StringContextData(File.ReadAllText(responsePath));
            return 0;
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
                    // Containers = containers.Select(c => new ContainerHookContainer { ContainerId = c.ContainerId, ContainerNetwork = c.ContainerNetwork }).ToList()
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
            return handler.ExecutionContext.Result == TaskResult.Failed || handler.ExecutionContext.Result == TaskResult.Canceled ? 1 : 0;
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