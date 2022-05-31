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
        private const string ResponseFolderName = "_runner_hook_responses";
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
            PrepareJobResponse response;
            try
            {
                response = await ExecuteHookScript<PrepareJobResponse>(context, input, ActionRunStage.Pre, prependPath);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                throw new Exception($"Custom container implementation failed with error: {ex.Message}. Please contact your self hosted runner administrator.");
            }
            jobContainer.IsAlpine = response.IsAlpine.Value;
            SaveHookState(context, response.State, input);
            UpdateJobContext(context, jobContainer, serviceContainers, response);
        }

        public async Task CleanupJobAsync(IExecutionContext context, List<ContainerInfo> containers)
        {
            Trace.Entering();

            var responsePath = GenerateResponsePath();
            var input = new HookInput
            {
                Command = HookCommand.CleanupJob,
                ResponseFile = responsePath,
                State = context.Global.ContainerHookState,
            };
            var prependPath = GetPrependPath(context);
            try
            {
                await ExecuteHookScript<PrepareJobResponse>(context, input, ActionRunStage.Pre, prependPath);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                throw new Exception($"Custom container implementation failed with error: {ex.Message}. Please contact your self hosted runner administrator.");
            }
        }

        public async Task ContainerStepAsync(IExecutionContext context, ContainerInfo container)
        {
            Trace.Entering();
            var responsePath = GenerateResponsePath();
            var hookState = context.Global.ContainerHookState;
            var input = new HookInput
            {
                Args = container.GetHookContainer(),
                Command = HookCommand.RunContainerStep,
                ResponseFile = responsePath,
                State = hookState
            };
            var prependPath = GetPrependPath(context);
            PrepareJobResponse response;
            try
            {
                response = await ExecuteHookScript<PrepareJobResponse>(context, input, ActionRunStage.Pre, prependPath);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                throw new Exception($"Custom container implementation failed with error: {ex.Message}. Please contact your self hosted runner administrator.");
            }
            if (response == null)
            {
                return;
            }
            SaveHookState(context, response.State, input);
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
                    EntryPointArgs = entryPointArgs.Split(' ').Select(arg => arg.Trim()),
                    EntryPoint = entryPoint,
                    EnvironmentVariables = environmentVariables,
                    PrependPath = prependPath,
                    WorkingDirectory = workingDirectory,
                },
                State = context.Global.ContainerHookState
            };

            PrepareJobResponse response;
            try
            {
                response = await ExecuteHookScript<PrepareJobResponse>(context, input, ActionRunStage.Pre, prependPath);
            }
            catch (Exception ex)
            {
                Trace.Error(ex);
                throw new Exception($"Custom container implementation failed with error: {ex.Message}. Please contact your self hosted runner administrator.");
            }
            if (response == null)
            {
                return;
            }
            SaveHookState(context, response.State, input);
        }

        private async Task<T> ExecuteHookScript<T>(IExecutionContext context, HookInput input, ActionRunStage stage, string prependPath) where T: HookResponse
        {
            var scriptDirectory = Path.GetDirectoryName(HookIndexPath);
            var stepHost = HostContext.CreateService<IDefaultStepHost>();
            Dictionary<string, string> inputs = new()
            {
                ["standardInInput"] = JsonUtility.ToString(input),
                ["path"] = HookIndexPath,
                ["shell"] = HostContext.GetDefaultShellForScript(HookIndexPath, Trace, prependPath)
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
                throw new Exception($"The hook script at '{HookIndexPath}' running command '{input.Command}' did not execute successfully."); // TODO: better exception
            }
            var response = GetResponse<T>(input);
            return response;
        }

        private string GenerateResponsePath()
        {
            return Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Temp), ResponseFolderName, $"{Guid.NewGuid()}.json");
        }

        private string GetPrependPath(IExecutionContext context)
        {
            return string.Join(Path.PathSeparator.ToString(), context.Global.PrependPath.Reverse<string>()); ;
        }

        private T GetResponse<T>(HookInput input) where T: HookResponse
        {
            T response = null;

            if (!string.IsNullOrEmpty(input.ResponseFile) && File.Exists(input.ResponseFile))
            {
                response = IOUtil.LoadObject<T>(input.ResponseFile);
                IOUtil.DeleteFile(input.ResponseFile);
                Trace.Info($"Response file for the hook script at '{HookIndexPath}' running command '{input.Command}' successfully processed and deleted.");
                response?.Validate();
            }
            else
            {
                Trace.Info($"Response file for the hook script at '{HookIndexPath}' running command '{input.Command}' not found.");
                if (input.Command == HookCommand.PrepareJob)
                {
                    throw new Exception($"Response file is required but not found for the hook script at '{HookIndexPath}' running command '{input.Command}'");
                }
            }

            if (response == null)
            {
                throw new Exception($"Response file is required but not found for the hook script at '{HookIndexPath}' running command '{input.Command}'");
            }
            return response;
        }

        private void SaveHookState(IExecutionContext context, JToken hookState, HookInput input)
        {
            if (hookState == null)
            {
                Trace.Info($"No 'state' property found in response file for '{input.Command}'. Global variable for 'ContainerHookState' will not be updated.");
                return;
            }
            context.Global.ContainerHookState = hookState;
            Trace.Info($"Global variable 'ContainerHookState' updated successfully for '{input.Command}' with data found in 'state' property of the response file.");
        }

        private void UpdateJobContext(IExecutionContext context, ContainerInfo jobContainer, List<ContainerInfo> serviceContainers, PrepareJobResponse response)
        {
            if (response.Context == null)
            {
                Trace.Info($"The response file does not contain a context. The fields 'jobContext.Container' and 'jobContext.Services' will not be set.");
                return;
            }

            var containerId = response.Context.Container?.Id;
            if (containerId != null)
            {
                context.JobContext.Container["id"] = new StringContextData(containerId);
                jobContainer.ContainerId = containerId;
            }

            var containerNetwork = response.Context.Container?.Network;
            if (containerNetwork != null)
            {
                context.JobContext.Container["network"] = new StringContextData(containerNetwork);
                jobContainer.ContainerNetwork = containerNetwork;
            }

            for (var i = 0; i < response.Context.Services.Count; i++)
            {
                var responseContainerInfo = response.Context.Services[i];
                var globalContainerInfo = serviceContainers[i];
                globalContainerInfo.ContainerId = responseContainerInfo.Id;
                globalContainerInfo.ContainerNetwork = responseContainerInfo.Network;

                var service = new DictionaryContextData()
                {
                    ["id"] = new StringContextData(responseContainerInfo.Id),
                    ["ports"] = new DictionaryContextData(),
                    ["network"] = new StringContextData(responseContainerInfo.Network)
                };

                globalContainerInfo.AddPortMappings(DockerUtil.ParseDockerPort(responseContainerInfo.Ports));
                foreach (var portMapping in globalContainerInfo.UserPortMappings)
                {
                    (service["ports"] as DictionaryContextData)[$"{portMapping.Key}:{portMapping.Value}"] = new StringContextData($"{portMapping.Key}:{portMapping.Value}");
                }
                context.JobContext.Services[globalContainerInfo.ContainerNetworkAlias] = service;
            }
        }
    }
}
