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
using Newtonsoft.Json.Linq;

namespace GitHub.Runner.Worker.Container.ContainerHooks
{
    [ServiceLocator(Default = typeof(ContainerHookManager))]
    public interface IContainerHookManager : IRunnerService
    {
        Task PrepareJobAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task CleanupJobAsync(IExecutionContext context, List<ContainerInfo> containers);
        Task ContainerStepAsync(IExecutionContext context, ContainerInfo container);
        Task ScriptStepAsync(IExecutionContext context, ContainerInfo container, string arguments, string fileName, IDictionary<string, string> environment, string prependPath, string workingDirectory);
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

            var prependPath = GetPrependPath(context);
            var response = await ExecuteHookScript(context, input, ActionRunStage.Pre, prependPath);
            if (response == null)
            {
                return;
            }

            var containerId = response?.Context?.Container?.Id;
            if (containerId != null)
            {
                context.JobContext.Container["id"] = new StringContextData(containerId);
                jobContainer.ContainerId = containerId;
            }

            var containerNetwork = response?.Context?.Container?.Network;
            if (containerNetwork != null)
            {
                context.JobContext.Container["network"] = new StringContextData(containerNetwork);
                jobContainer.ContainerNetwork = containerNetwork;
            }
            
            if (response.IsAlpine == null)
            {
                throw new Exception("Expected field 'alpine' was not returned. Please contact your self hosted runner administrator.");
            }
            jobContainer.IsAlpine = (bool)response.IsAlpine;

            SaveHookState(context, response.State);

            for (var i = 0; i < response?.Context?.Services?.Count; i++)
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

                container.PortMappings = new Dictionary<string, string>();
                foreach (var portMapping in containerInfo.UserPortMappings)
                {
                    (service["ports"] as DictionaryContextData)[$"{portMapping.Key}:{portMapping.Value}"] = new StringContextData($"{portMapping.Key}:{portMapping.Value}");
                    container.PortMappings.Add(portMapping);
                }
                context.JobContext.Services[containerInfo.ContainerNetworkAlias] = service;
            }
        }

        public async Task CleanupJobAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();

            var responsePath = GenerateResponsePath();
            var input = new HookInput
            {
                Command = HookCommand.CleanupJob,
                ResponseFile = responsePath,
                State = GetHookStateInJson(context),
            };
            var prependPath = GetPrependPath(context);
            var response = await ExecuteHookScript(context, input, ActionRunStage.Post, prependPath);
            if (response == null)
            {
                return;
            }
            SaveHookState(context, response.State);
        }

        public async Task ContainerStepAsync(IExecutionContext context, ContainerInfo container)
        {
            Trace.Entering();
            var responsePath = GenerateResponsePath();
            var hookState = GetHookStateInJson(context);
            var input = new HookInput
            {
                Args =  container.GetHookContainer(),
                Command = HookCommand.RunContainerStep,
                ResponseFile = responsePath,
                State = hookState
            };
            var prependPath = GetPrependPath(context);
            var response = await ExecuteHookScript(context, input, ActionRunStage.Post, prependPath);
            if (response == null)
            {
                return;
            }
            SaveHookState(context, response.State);
        }

        public async Task ScriptStepAsync(IExecutionContext context, ContainerInfo container, string entryPointArgs, string entryPoint, IDictionary<string, string> environmentVariables, string prependPath, string workingDirectory)
        {
            Trace.Entering();
            var responsePath = GenerateResponsePath();
            var input = new HookInput
            {
                Command = HookCommand.RunScriptStep,
                ResponseFile = responsePath,
                Args = new ScriptStepArgs
                {
                    Container = container.GetHookContainer(),
                    EntryPointArgs = entryPointArgs.Split(' ').Select(arg => arg.Trim()),
                    EntryPoint = entryPoint,
                    EnvironmentVariables = environmentVariables,
                    PrependPath = prependPath,
                    WorkingDirectory = workingDirectory,
                },
                State = GetHookStateInJson(context)
            };

            var response = await ExecuteHookScript(context, input, ActionRunStage.Main, prependPath);
            if (response == null)
            {
                return;
            }
            SaveHookState(context, response.State);
        }

        private async Task<HookResponse> ExecuteHookScript(IExecutionContext context, HookInput input, ActionRunStage stage, string prependPath)
        {
            var scriptDirectory = Path.GetDirectoryName(HookIndexPath);
            var stepHost = HostContext.CreateService<IDefaultStepHost>();
            Dictionary<string, string> inputs = new()
            {
                ["standardInInput"] = JsonUtility.ToString(input),
                ["path"] = HookIndexPath,
                ["shell"] = ScriptHandlerHelpers.GetDefaultShellForScript(HookIndexPath, Trace, prependPath, HostContext)
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
            handler.PrepareExecution(stage);

            IOUtil.CreateEmptyFile(input.ResponseFile);
            await handler.RunAsync(stage);
            if (handler.ExecutionContext.Result == TaskResult.Failed)
            {
                throw new Exception("Hook failed"); // TODO: better exception
            }

            HookResponse response = null;
            if (!string.IsNullOrEmpty(input.ResponseFile) && File.Exists(input.ResponseFile))
            {
                response = IOUtil.LoadObject<HookResponse>(input.ResponseFile);
                IOUtil.DeleteFile(input.ResponseFile);
                Trace.Info("Response file successfully processed and deleted");
            }
            else
            {
                Trace.Info($"Response file not found for command '{input.Command}'");
            }
            return response;
        }

        private string GenerateResponsePath()
        {
            return Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), ResponseFolderName, $"{Guid.NewGuid()}.json");
        }

        private string GetPrependPath(IExecutionContext context)
        {
            return string.Join(Path.PathSeparator.ToString(), context.Global.PrependPath.Reverse<string>());;
        }

        private static JToken GetHookStateInJson(IExecutionContext context)
        {
            if (context.JobContext.TryGetValue("hook_state", out var hookState))
            {
                // TODO: log state read & loaded
                return JsonUtility.FromString<JToken>(hookState.ToString());
            }
            return JToken.Parse("{}");
        }

        private static void SaveHookState(IExecutionContext context, JToken hookState)
        {
            // TODO: consider JTokenContextData
            hookState ??= JToken.Parse("{}");
            context.JobContext["hook_state"] = new StringContextData(JsonUtility.ToString(hookState));
        }
    }
}
