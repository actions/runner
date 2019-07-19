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
                var buildExitCode = await dockerManger.DockerBuild(ExecutionContext, ExecutionContext.GetGitHubContext("workspace"), Directory.GetParent(dockerFile).FullName, imageName);
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

            if (!string.IsNullOrEmpty(Data.EntryPoint))
            {
                container.ContainerEntryPoint = Inputs.GetValueOrDefault(Data.EntryPoint);
            }
            else
            {
                container.ContainerEntryPoint = Inputs.GetValueOrDefault("entryPoint");
            }

            if (Data.Arguments != null)
            {
                container.ContainerEntryPointArgs = "";
                foreach (var arg in Data.Arguments)
                {
                    var value = Inputs.GetValueOrDefault(arg);
                    if (!string.IsNullOrEmpty(value))
                    {
                        container.ContainerEntryPointArgs = container.ContainerEntryPointArgs + $" \"{value.Replace("\"", "\\\"")}\"";
                    }
                }
            }
            else
            {
                container.ContainerEntryPointArgs = Inputs.GetValueOrDefault("args");
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

            var tempWorkflowDirectory = Path.Combine(tempDirectory, "_github_workflow");
            ArgUtil.Directory(tempWorkflowDirectory, nameof(tempWorkflowDirectory));

            container.MountVolumes.Add(new MountVolume("/var/run/docker.sock", "/var/run/docker.sock"));
            container.MountVolumes.Add(new MountVolume(tempHomeDirectory, "/github/home"));
            container.MountVolumes.Add(new MountVolume(tempWorkflowDirectory, "/github/workflow"));
            container.MountVolumes.Add(new MountVolume(defaultWorkingDirectory, "/github/workspace"));

            container.AddPathTranslateMapping(tempHomeDirectory, "/github/home");
            container.AddPathTranslateMapping(tempWorkflowDirectory, "/github/workflow");
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

            foreach (var variable in this.Environment)
            {
                container.ContainerEnvironmentVariables[variable.Key] = container.TranslateToContainerPath(variable.Value);
            }

            using (var stdoutManager = new OutputManager(ExecutionContext, ActionCommandManager))
            using (var stderrManager = new OutputManager(ExecutionContext, ActionCommandManager))
            {
                var runExitCode = await dockerManger.DockerRun(ExecutionContext, container, stdoutManager.OnDataReceived, stderrManager.OnDataReceived);
                if (runExitCode != 0)
                {
                    ExecutionContext.Error($"Docker run failed with exit code {runExitCode}");
                    ExecutionContext.Result = TaskResult.Failed;
                }
            }
        }
    }
}
