using Microsoft.VisualStudio.Services.Agent.Util;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;
using Agent.Sdk;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Build.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Handlers
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
                ArgUtil.File(Data.Target, nameof(Data.Target));
                ExecutionContext.Output($"Dockerfile for action: '{Data.Target}'.");

                var imageName = $"{dockerManger.DockerInstanceLabel}:{ExecutionContext.Id.ToString("N")}";
                var buildExitCode = await dockerManger.DockerBuild(ExecutionContext, Directory.GetParent(Data.Target).FullName, imageName);
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

            var defaultWorkingDirectory = ExecutionContext.Variables.System_DefaultWorkingDirectory;
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

            // populate action environment variables.
            var selfRepo = ExecutionContext.Repositories.Single(x => string.Equals(x.Alias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase) ||
                                                                     string.Equals(x.Alias, Pipelines.PipelineConstants.DesignerRepo, StringComparison.OrdinalIgnoreCase));

            // GITHUB_ACTOR=ericsciple
            container.ContainerEnvironmentVariables["GITHUB_ACTOR"] = selfRepo.Properties.Get<Pipelines.VersionInfo>(Pipelines.RepositoryPropertyNames.VersionInfo)?.Author ?? string.Empty;

            // GITHUB_REPOSITORY=bryanmacfarlane/actionstest
            container.ContainerEnvironmentVariables["GITHUB_REPOSITORY"] = selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Name, string.Empty);

            // GITHUB_WORKSPACE=/github/workspace
            container.ContainerEnvironmentVariables["GITHUB_WORKSPACE"] = "/github/workspace";

            // GITHUB_SHA=1a204f473f6001b7fac9c6453e76702f689a41a9
            container.ContainerEnvironmentVariables["GITHUB_SHA"] = selfRepo.Version;

            // GITHUB_REF=refs/heads/master
            container.ContainerEnvironmentVariables["GITHUB_REF"] = selfRepo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Ref, string.Empty);

            // GITHUB_TOKEN=TOKEN
            if (selfRepo.Endpoint != null)
            {
                var repoEndpoint = ExecutionContext.Endpoints.FirstOrDefault(x => x.Id == selfRepo.Endpoint.Id);
                if (repoEndpoint?.Authorization?.Parameters != null && repoEndpoint.Authorization.Parameters.ContainsKey("accessToken"))
                {
                    container.ContainerEnvironmentVariables["GITHUB_TOKEN"] = repoEndpoint.Authorization.Parameters["accessToken"];
                }
            }

            // HOME=/github/home
            container.ContainerEnvironmentVariables["HOME"] = "/github/home";

            // GITHUB_WORKFLOW=test on push
            container.ContainerEnvironmentVariables["GITHUB_WORKFLOW"] = ExecutionContext.Variables.Build_DefinitionName;

            // GITHUB_EVENT_NAME=push
            container.ContainerEnvironmentVariables["GITHUB_EVENT_NAME"] = ExecutionContext.Variables.Get(BuildVariables.Reason);

            // GITHUB_ACTION=dump.env
            // GITHUB_EVENT_PATH=/github/workflow/event.json

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
