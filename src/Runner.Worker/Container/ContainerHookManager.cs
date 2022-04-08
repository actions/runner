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
        Task<int> CleanupJobAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task<int> ContainerStepAsync(IExecutionContext context);
        Task<int> RunScriptStepAsync(IExecutionContext context);
    }

    public class ContainerHookManager : RunnerService, IContainerHookManager
    {
        public async Task<int> PrepareJobAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();

            var hookIndexPath = HostContext.GetDirectory(WellKnownDirectory.ContainerHooks);
            var responsePath = $"{hookIndexPath}/response.json";
            using (StreamWriter w = File.AppendText(responsePath)) { } // create if not file exists
            var jobContainer = containers.Where(c => c.IsJobContainer).FirstOrDefault();
            var serviceContainers = containers.Where(c => c.IsJobContainer == false).ToList();

            var meta = new ContainerHookMeta
            {
                Command = "prepare_job", // TODO: work out   GetHookCommand(nameof(PrepareJobAsync))
                ResponseFile = responsePath,
                Args = new ContainerHookArgs
                {
                    JobContainer = jobContainer.GetHookContainer(),
                    Services = serviceContainers.Select(c => c.GetHookContainer()).ToList(),
                }
            };
            var exitCode = await ExecuteHookScript(context, GetHookIndexPath(), meta);
            if (exitCode != 0)
            {
                throw new Exception("Hook failed"); // TODO: fail or fallback?
            }

            var response = JsonUtility.FromString<ContainerHookResponse>(await File.ReadAllTextAsync(responsePath));
            File.Delete(responsePath);

            context.JobContext.Container["id"] = new StringContextData(response.Context.Container.Id);
            jobContainer.ContainerId = response.Context.Container.Id;
            context.JobContext.Container["network"] = new StringContextData(response.Context.Container.Network);
            jobContainer.ContainerNetwork = response.Context.Container.Network;
            // var configEnvFormat = "--format \"{{range .Config.Env}}{{println .}}{{end}}\"";
            // var containerEnv = await _dockerManager.DockerInspect(executionContext, container.ContainerId, configEnvFormat);
            // container.ContainerRuntimePath = DockerUtil.ParsePathFromConfigEnv(containerEnv);

            for (var i = 0; i < response.Context.Services.Count; i++)
            {
                var container = response.Context.Services[i]; // TODO: Confirm that the order is stable
                var containerInfo = serviceContainers[i];
                containerInfo.ContainerId = container.Id;
                containerInfo.ContainerNetwork = container.Network;
                var service = new DictionaryContextData()
                {
                    ["id"] = new StringContextData(container.Id),
                    ["ports"] = new DictionaryContextData(),
                    ["network"] = new StringContextData(container.Network)
                };

                // TODO: workout port mappings + format
                // foreach (var portMapping in containerInfo.UserPortMappings)
                // {
                //     // TODO: currently the format is ports["80:8080"] = "80:8080", fix this?
                //     (service["ports"] as DictionaryContextData)[$"{portMapping.Key}:{portMapping.Value}"] = new StringContextData($"{portMapping.Key}:{portMapping.Value}");
                // }
                context.JobContext.Services[containerInfo.ContainerNetworkAlias] = service;
            }

            return 0;
        }

        public async Task<int> CleanupJobAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();

            var meta = new ContainerHookMeta
            {
                Command = "cleanup_job", // GetHookCommand(nameof(CleanupJobAsync)),
                ResponseFile = "response.json",
                Args = new ContainerHookArgs
                {
                    JobContainer = containers.Where(c => c.IsJobContainer).FirstOrDefault().GetHookContainer(),
                    Services = containers.Where(c => c.IsJobContainer == false).Select(c => c.GetHookContainer()).ToList(),
                    Network = containers.Where(c => !string.IsNullOrEmpty(c.ContainerNetwork)).FirstOrDefault()?.ContainerNetwork,
                }
            };
            return await ExecuteHookScript(context, GetHookIndexPath(), meta);
        }

        public async Task<int> ContainerStepAsync(IExecutionContext context)
        {
            Trace.Entering();
            await Task.FromResult(0);
            throw new NotImplementedException();
        }

        public async Task<int> RunScriptStepAsync(IExecutionContext context)
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
            return Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath);
        }
    }
}