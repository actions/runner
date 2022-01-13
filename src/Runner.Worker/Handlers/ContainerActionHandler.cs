using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using GitHub.Runner.Worker.Container;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;
using GitHub.DistributedTask.Pipelines.ContextData;
using System.Linq;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(ContainerActionHandler))]
    public interface IContainerActionHandler : IHandler
    {
        ContainerActionExecutionData Data { get; set; }
    }

    public sealed class ContainerActionHandler : Handler, IContainerActionHandler
    {
        private static string GetHostOS() {
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) {
                return "linux";
            } else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                return "windows";
            } else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) {
                return "osx";
            }
            return null;
        }

        public ContainerActionExecutionData Data { get; set; }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously (method has async logic on only certain platforms)
        public async Task RunAsync(ActionRunStage stage)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

            RunnerContext runnerctx = null;
            try {
                runnerctx = ExecutionContext.ExpressionValues["runner"] as RunnerContext;
            } catch {

            }
            var dockerManager = HostContext.GetService<IDockerCommandManager>();

            if(!(StepHost is IContainerStepHost) && runnerctx != null) {
                ExecutionContext.ExpressionValues["runner"] = new RunnerContext();
                foreach(var entr in runnerctx) {
                    ExecutionContext.SetRunnerContext(entr.Key, entr.Value?.AssertString("runner ctx").Value);
                }
                await dockerManager.DockerVersion(ExecutionContext);
                var os = dockerManager.Os;
                ExecutionContext.SetRunnerContext("os", os.First().ToString().ToUpper() + os.Substring(1));
                ExecutionContext.SetRunnerContext("tool_cache", Path.Combine(Path.GetDirectoryName(runnerctx["tool_cache"].AssertString("runner ctx").Value), os));
            }

            // Update the env dictionary.
            AddInputsToEnvironment();

            // container image haven't built/pull
            if (Data.Image.StartsWith("docker://", StringComparison.OrdinalIgnoreCase))
            {
                Data.Image = Data.Image.Substring("docker://".Length);
            }
            else if (Data.Image.EndsWith("Dockerfile") || Data.Image.EndsWith("dockerfile"))
            {
                // ensure docker file exist
                var dockerFile = Path.Combine(ActionDirectory, Data.Image);
                ArgUtil.File(dockerFile, nameof(Data.Image));

                ExecutionContext.Output($"##[group]Building docker image");
                ExecutionContext.Output($"Dockerfile for action: '{dockerFile}'.");
                var imageName = $"{dockerManager.DockerInstanceLabel}:{ExecutionContext.Id.ToString("N")}";
                var buildExitCode = await dockerManager.DockerBuild(
                    ExecutionContext,
                    ExecutionContext.GetGitHubContext("workspace"),
                    dockerFile,
                    Directory.GetParent(dockerFile).FullName,
                    imageName);
                ExecutionContext.Output("##[endgroup]");

                if (buildExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker build failed with exit code {buildExitCode}");
                }

                Data.Image = imageName;
            }

            string type = Action.Type == Pipelines.ActionSourceType.Repository ? "Dockerfile" : "DockerHub";
            // Add Telemetry to JobContext to send with JobCompleteMessage
            if (stage == ActionRunStage.Main)
            {
                var telemetry = new ActionsStepTelemetry {
                    Ref = GetActionRef(),
                    HasPreStep = Data.HasPre,
                    HasPostStep = Data.HasPost,
                    IsEmbedded = ExecutionContext.IsEmbedded,
                    Type = type
                };
                ExecutionContext.Root.ActionsStepsTelemetry.Add(telemetry);
            }

            // run container
            var container = new ContainerInfo(HostContext)
            {
                ContainerImage = Data.Image,
                ContainerName = ExecutionContext.Id.ToString("N"),
                ContainerDisplayName = $"{Pipelines.Validation.NameValidation.Sanitize(Data.Image)}_{Guid.NewGuid().ToString("N").Substring(0, 6)}",
            };

            if (stage == ActionRunStage.Main)
            {
                if (!string.IsNullOrEmpty(Data.EntryPoint))
                {
                    // use entrypoint from action.yml
                    container.ContainerEntryPoint = Data.EntryPoint;
                }
                else
                {
                    // use entrypoint input, this is for action v1 which doesn't have action.yml
                    container.ContainerEntryPoint = Inputs.GetValueOrDefault("entryPoint");
                }
            }
            else if (stage == ActionRunStage.Pre)
            {
                container.ContainerEntryPoint = Data.Pre;
            }
            else if (stage == ActionRunStage.Post)
            {
                container.ContainerEntryPoint = Data.Post;
            }

            // create inputs context for template evaluation
            var inputsContext = new DictionaryContextData();
            if (this.Inputs != null)
            {
                foreach (var input in Inputs)
                {
                    inputsContext.Add(input.Key, new StringContextData(input.Value));
                }
            }

            var extraExpressionValues = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
            extraExpressionValues["inputs"] = inputsContext;

            var manifestManager = HostContext.GetService<IActionManifestManager>();
            if (Data.Arguments != null)
            {
                container.ContainerEntryPointArgs = "";
                var evaluatedArgs = manifestManager.EvaluateContainerArguments(ExecutionContext, Data.Arguments, extraExpressionValues);
                foreach (var arg in evaluatedArgs)
                {
                    if (!string.IsNullOrEmpty(arg))
                    {
                        container.ContainerEntryPointArgs = container.ContainerEntryPointArgs + $" \"{arg.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
                    }
                    else
                    {
                        container.ContainerEntryPointArgs = container.ContainerEntryPointArgs + " \"\"";
                    }
                }
            }
            else
            {
                container.ContainerEntryPointArgs = Inputs.GetValueOrDefault("args");
            }

            if (Data.Environment != null)
            {
                var evaluatedEnv = manifestManager.EvaluateContainerEnvironment(ExecutionContext, Data.Environment, extraExpressionValues);
                foreach (var env in evaluatedEnv)
                {
                    if (!this.Environment.ContainsKey(env.Key))
                    {
                        this.Environment[env.Key] = env.Value;
                    }
                }
            }

            if (ExecutionContext.JobContext.Container.TryGetValue("network", out var networkContextData) && networkContextData is StringContextData networkStringData)
            {
                container.ContainerNetwork = networkStringData.ToString();
            }

            var defaultWorkingDirectory = ExecutionContext.GetGitHubContext("workspace");
            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);

            ArgUtil.NotNullOrEmpty(defaultWorkingDirectory, nameof(defaultWorkingDirectory));
            ArgUtil.NotNullOrEmpty(tempDirectory, nameof(tempDirectory));

            var tempHomeDirectory = Path.Combine(tempDirectory, "_github_home");
            Directory.CreateDirectory(tempHomeDirectory);
            this.Environment["HOME"] = tempHomeDirectory;

            var tempFileCommandDirectory = Path.Combine(tempDirectory, "_runner_file_commands");
            ArgUtil.Directory(tempFileCommandDirectory, nameof(tempFileCommandDirectory));

            var tempWorkflowDirectory = Path.Combine(tempDirectory, "_github_workflow");
            ArgUtil.Directory(tempWorkflowDirectory, nameof(tempWorkflowDirectory));

            string prefix = "";
            if(HostContext.GetService<IDockerCommandManager>().Os == "windows") {
                prefix = "c:";
            } else {
                container.MountVolumes.Add(new MountVolume("/var/run/docker.sock", "/var/run/docker.sock"));
            }
            container.MountVolumes.Add(new MountVolume(tempHomeDirectory, prefix + "/github/home"));
            container.MountVolumes.Add(new MountVolume(tempWorkflowDirectory, prefix + "/github/workflow"));
            container.MountVolumes.Add(new MountVolume(tempFileCommandDirectory, prefix + "/github/file_commands"));
            container.MountVolumes.Add(new MountVolume(defaultWorkingDirectory, prefix + "/github/workspace"));

            container.AddPathTranslateMapping(tempHomeDirectory, prefix + "/github/home");
            container.AddPathTranslateMapping(tempWorkflowDirectory, prefix + "/github/workflow");
            container.AddPathTranslateMapping(tempFileCommandDirectory, prefix + "/github/file_commands");
            container.AddPathTranslateMapping(defaultWorkingDirectory, prefix + "/github/workspace");

            container.ContainerWorkDirectory = prefix + "/github/workspace";

            // expose context to environment
            foreach (var context in ExecutionContext.ExpressionValues)
            {
                if (context.Value is IEnvironmentContextData runtimeContext && runtimeContext != null)
                {
                    foreach (var env in runtimeContext.GetRuntimeEnvironmentVariables())
                    {
                        Environment[env.Key] = env.Value;
                    }
                }
            }

            // Add Actions Runtime server info
            var systemConnection = ExecutionContext.Global.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            Environment["ACTIONS_RUNTIME_URL"] = systemConnection.Url.AbsoluteUri;
            Environment["ACTIONS_RUNTIME_TOKEN"] = systemConnection.Authorization.Parameters[EndpointAuthorizationParameters.AccessToken];
            if (systemConnection.Data.TryGetValue("CacheServerUrl", out var cacheUrl) && !string.IsNullOrEmpty(cacheUrl))
            {
                Environment["ACTIONS_CACHE_URL"] = cacheUrl;
            }
            if (systemConnection.Data.TryGetValue("GenerateIdTokenUrl", out var generateIdTokenUrl) && !string.IsNullOrEmpty(generateIdTokenUrl))
            {
                Environment["ACTIONS_ID_TOKEN_REQUEST_URL"] = generateIdTokenUrl;
                Environment["ACTIONS_ID_TOKEN_REQUEST_TOKEN"] = systemConnection.Authorization.Parameters[EndpointAuthorizationParameters.AccessToken];
            }

            foreach (var variable in this.Environment)
            {
                container.ContainerEnvironmentVariables[variable.Key] = container.TranslateToContainerPath(variable.Value);
            }

            using (var stdoutManager = new OutputManager(ExecutionContext, ActionCommandManager, container))
            using (var stderrManager = new OutputManager(ExecutionContext, ActionCommandManager, container))
            {
                var runExitCode = await dockerManager.DockerRun(ExecutionContext, container, stdoutManager.OnDataReceived, stderrManager.OnDataReceived);
                ExecutionContext.Debug($"Docker Action run completed with exit code {runExitCode}");
                if (runExitCode != 0)
                {
                    ExecutionContext.Result = TaskResult.Failed;
                }
            }
            if(runnerctx != null) {
                ExecutionContext.ExpressionValues["runner"] = runnerctx;
            }
        }
    }
}
