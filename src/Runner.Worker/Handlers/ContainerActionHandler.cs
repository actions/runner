using GitHub.Runner.Common.Util;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
using GitHub.Runner.Worker.Container;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(ContainerActionHandler))]
    public interface IContainerActionHandler : IHandler
    {
        ContainerActionHandlerData Data { get; set; }
    }

    public sealed class ContainerActionHandler : Handler, IContainerActionHandler
    {
        public ContainerActionHandlerData Data { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(Data, nameof(Data));
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

            var dockerManger = HostContext.GetService<IDockerCommandManager>();

            // container image haven't built
            if (string.IsNullOrEmpty(Data.ContainerImage))
            {
                // ensure docker file exist
                var dockerFile = Path.Combine(TaskDirectory, Data.Target);
                ArgUtil.File(dockerFile, nameof(Data.Target));
                ExecutionContext.Output($"Dockerfile for action: '{dockerFile}'.");

                var imageName = $"{dockerManger.DockerInstanceLabel}:{ExecutionContext.Id.ToString("N")}";
                var buildExitCode = await dockerManger.DockerBuild(ExecutionContext, Directory.GetParent(dockerFile).FullName, imageName);
                if (buildExitCode != 0)
                {
                    throw new InvalidOperationException($"Docker build failed with exit code {buildExitCode}");
                }

                Data.ContainerImage = imageName;
            }

            // run container
            var container = new ContainerInfo()
            {
                ContainerImage = Data.ContainerImage,
                ContainerName = ExecutionContext.Id.ToString("N"),
                ContainerDisplayName = $"{Pipelines.Validation.NameValidation.Sanitize(Data.ContainerImage)}_{Guid.NewGuid().ToString("N").Substring(0, 6)}",
            };

            container.ContainerEntryPoint = Inputs.GetValueOrDefault("entryPoint");
            container.ContainerCommand = Inputs.GetValueOrDefault("args");

            if (ExecutionContext.Variables.TryGetValue("Agent.ContainerNetwork", out string containerNetwork))
            {
                container.ContainerNetwork = containerNetwork;
            }

            var githubContext = ExecutionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));
            var defaultWorkingDirectory = githubContext["workspace"] as StringContextData;
            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);

            ArgUtil.NotNullOrEmpty(defaultWorkingDirectory, nameof(defaultWorkingDirectory));
            ArgUtil.NotNullOrEmpty(tempDirectory, nameof(tempDirectory));

            var tempHomeDirectory = Path.Combine(tempDirectory, "_github_home");
            Directory.CreateDirectory(tempHomeDirectory);

            var tempWorkflowDirectory = Path.Combine(tempDirectory, "_github_workflow");
            Directory.CreateDirectory(tempWorkflowDirectory);

            // _pathMappings[defaultWorkingDirectory] = "/github/workspace";
            // _pathMappings[tempHomeDirectory] = "/github/home";

            container.MountVolumes.Add(new MountVolume("/var/run/docker.sock", "/var/run/docker.sock"));
            container.MountVolumes.Add(new MountVolume(tempHomeDirectory, "/github/home"));
            container.MountVolumes.Add(new MountVolume(tempWorkflowDirectory, "/github/workflow"));
            container.MountVolumes.Add(new MountVolume(defaultWorkingDirectory, "/github/workspace"));

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

            foreach (var variable in this.Environment)
            {
                container.ContainerEnvironmentVariables[variable.Key] = container.TranslateToContainerPath(variable.Value);
            }

            var runExitCode = await dockerManger.DockerRun(ExecutionContext, container);
            if (runExitCode != 0)
            {
                throw new InvalidOperationException($"Docker run failed with exit code {runExitCode}");
            }
        }

        private void OnDataReceived(object sender, ProcessDataReceivedEventArgs e)
        {
            // This does not need to be inside of a critical section.
            // The logging queues and command handlers are thread-safe.
            if (!ActionCommandManager.TryProcessCommand(ExecutionContext, e.Data))
            {
                ExecutionContext.Output(e.Data);
            }
        }
    }
}
