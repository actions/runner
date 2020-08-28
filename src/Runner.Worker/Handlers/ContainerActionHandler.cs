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
        public ContainerActionExecutionData Data { get; set; }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously (method has async logic on only certain platforms)
        public async Task RunAsync(ActionRunStage stage)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

#if OS_WINDOWS || OS_OSX
            throw new NotSupportedException($"Container action is only supported on Linux");
#else
            // Update the env dictionary.
            AddInputsToEnvironment();

            var dockerManger = HostContext.GetService<IDockerCommandManager>();

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
                var imageName = $"{dockerManger.DockerInstanceLabel}:{ExecutionContext.Id.ToString("N")}";
                var buildExitCode = await dockerManger.DockerBuild(
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

            // run container
            var container = new ContainerInfo()
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
                        container.ContainerEntryPointArgs = container.ContainerEntryPointArgs + $" \"{arg.Replace("\"", "\\\"")}\"";
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

            container.MountVolumes.Add(new MountVolume("/var/run/docker.sock", "/var/run/docker.sock"));
            container.MountVolumes.Add(new MountVolume(tempHomeDirectory, "/github/home"));
            container.MountVolumes.Add(new MountVolume(tempWorkflowDirectory, "/github/workflow"));
            container.MountVolumes.Add(new MountVolume(tempFileCommandDirectory, "/github/file_commands"));
            container.MountVolumes.Add(new MountVolume(defaultWorkingDirectory, "/github/workspace"));

            container.AddPathTranslateMapping(tempHomeDirectory, "/github/home");
            container.AddPathTranslateMapping(tempWorkflowDirectory, "/github/workflow");
            container.AddPathTranslateMapping(tempFileCommandDirectory, "/github/file_commands");
            container.AddPathTranslateMapping(defaultWorkingDirectory, "/github/workspace");

            container.ContainerWorkDirectory = "/github/workspace";

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

            foreach (var variable in this.Environment)
            {
                container.ContainerEnvironmentVariables[variable.Key] = container.TranslateToContainerPath(variable.Value);
            }

            using (var stdoutManager = new OutputManager(ExecutionContext, ActionCommandManager, container))
            using (var stderrManager = new OutputManager(ExecutionContext, ActionCommandManager, container))
            {
                var runExitCode = await dockerManger.DockerRun(ExecutionContext, container, stdoutManager.OnDataReceived, stderrManager.OnDataReceived);
                ExecutionContext.Debug($"Docker Action run completed with exit code {runExitCode}");
                if (runExitCode != 0)
                {
                    ExecutionContext.Result = TaskResult.Failed;
                }
            }
#endif
        }
    }
}
