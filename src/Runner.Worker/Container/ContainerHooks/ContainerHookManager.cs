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

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    [ServiceLocator(Default = typeof(ContainerHookManager))]
    public interface IContainerHookManager : IRunnerService
    {
        Task PrepareJobAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task CleanupJobAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task ContainerStepAsync(IExecutionContext context);
        Task RunScriptStepOnJobContainerAsync(IExecutionContext context, ContainerInfo container, string arguments, string fileName, IDictionary<string, string> environment, string prependPath, string workingDirectory);
    }

    public class ContainerHookManager : RunnerService, IContainerHookManager
    {
        private const string ResponseFolderName = "_hook_responses";
        private string HookIndexPath;
        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            HookIndexPath = $"{Environment.GetEnvironmentVariable(Constants.Hooks.ContainerHooksPath)}";
        }

        public async Task PrepareJobAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();
            var responsePath = GenerateResponsePath();
            var jobContainer = containers.Where(c => c.IsJobContainer).FirstOrDefault();
            var serviceContainers = containers.Where(c => c.IsJobContainer == false).ToList();

            var input = new HookInput
            {
                Command = HookCommand.PrepareJob,
                ResponseFile = responsePath,
                Args = new PrepareJobArgs
                {
                    Container = jobContainer.GetHookContainer(),
                    Services = serviceContainers.Select(c => c.GetHookContainer()).ToList(),
                }
            };

            var response = await ExecuteHookScript(context, input);
            // TODO: Should we throw if response.Context is null or just noop?
            context.JobContext.Container["id"] = new StringContextData(response.Context.Container.Id);
            jobContainer.ContainerId = response.Context.Container.Id;
            context.JobContext.Container["network"] = new StringContextData(response.Context.Container.Network);
            jobContainer.ContainerNetwork = response.Context.Container.Network;
            context.JobContext["hook_state"] = new StringContextData(JsonUtility.ToString(response.State));

            // TODO: figure out if we need ContainerRuntimePath for anything
            // var configEnvFormat = "--format \"{{range .Config.Env}}{{println .}}{{end}}\"";
            // var containerEnv = await _dockerManager.DockerInspect(executionContext, container.ContainerId, configEnvFormat);
            // container.ContainerRuntimePath = DockerUtil.ParsePathFromConfigEnv(containerEnv);

            for (var i = 0; i < response.Context.Services.Count; i++)
            {
                var container = response.Context.Services[i]; // TODO: Confirm that the order response.Context.Services is the same as serviceContainers
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
        }

        public async Task CleanupJobAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();

            var responsePath = GenerateResponsePath();
            context.JobContext.TryGetValue("hook_state", out var hookState);
            var input = new HookInput
            {
                Command = HookCommand.CleanupJob,
                ResponseFile = responsePath,
                State = JsonUtility.FromString<dynamic>(hookState.ToString())
            };
            var response = await ExecuteHookScript(context, input);
        }

        public async Task ContainerStepAsync(IExecutionContext context)
        {
            Trace.Entering();
            await Task.FromResult(0);
            throw new NotImplementedException();
        }

        public async Task RunScriptStepOnJobContainerAsync(IExecutionContext context, ContainerInfo container, string entryPointArgs, string entryPoint, IDictionary<string, string> environmentVariables, string prependPath, string workingDirectory)
        {
            Trace.Entering();
            var responsePath = GenerateResponsePath();

            context.JobContext.TryGetValue("hook_state", out var hookState);
            var input = new HookInput
            {
                Command = HookCommand.RunScriptStep,
                ResponseFile = responsePath,
                Args = new HookStepArgs
                {
                    Container = container.GetHookContainer(),
                    EntryPointArgs = entryPointArgs.Split(' ').Select(arg => arg.Trim()),
                    EntryPoint = entryPoint,
                    EnvironmentVariables = environmentVariables,
                    PrependPath = prependPath,
                    WorkingDirectory = workingDirectory,
                },
                State = JsonUtility.FromString<dynamic>(hookState.ToString())
            };

            var response = await ExecuteHookScript(context, input);
            if (response != null) 
            {
                context.JobContext["hook_state"] = new StringContextData(JsonUtility.ToString(response.State));
            }
        }

        private async Task<HookResponse> ExecuteHookScript(IExecutionContext context, HookInput input)
        {
            var scriptDirectory = Path.GetDirectoryName(HookIndexPath);
            var stepHost = HostContext.CreateService<IDefaultStepHost>();
            Dictionary<string, string> inputs = new()
            {
                ["standardInInput"] = JsonUtility.ToString(input),
                ["path"] = HookIndexPath,
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

            IOUtil.CreateEmptyFile(input.ResponseFile);
            await handler.RunAsync(ActionRunStage.Pre);
            if (handler.ExecutionContext.Result == TaskResult.Failed)
            {
                throw new Exception("Hook failed"); // TODO: better exception
            }

            var response = IOUtil.LoadObject<HookResponse>(input.ResponseFile);
            IOUtil.DeleteFile(input.ResponseFile);
            return response;
        }

        private string GenerateResponsePath()
        {
            return $"{Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), ResponseFolderName)}/{Guid.NewGuid()}.json";
        }
    }
}