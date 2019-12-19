using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Plugins.Artifact
{
    public class DownloadArtifact : IRunnerActionPlugin
    {

        private static class DownloadArtifactInputNames
        {
            public static readonly string Name = "name";
            public static readonly string ArtifactName = "artifact";
            public static readonly string Path = "path";
        }

        public async Task RunAsync(
            RunnerActionPluginExecutionContext context,
            CancellationToken token)
        {
            ArgUtil.NotNull(context, nameof(context));
            string artifactName = context.GetInput(DownloadArtifactInputNames.ArtifactName, required: false); // Back compat since we rename input `artifact` to `name`
            if (string.IsNullOrEmpty(artifactName))
            {
                artifactName = context.GetInput(DownloadArtifactInputNames.Name, required: true);
            }

            string targetPath = context.GetInput(DownloadArtifactInputNames.Path, required: false);
            string defaultWorkingDirectory = context.GetGitHubContext("workspace");

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = artifactName;
            }

            targetPath = Path.IsPathFullyQualified(targetPath) ? targetPath : Path.GetFullPath(Path.Combine(defaultWorkingDirectory, targetPath));

            // Build ID
            string buildIdStr = context.Variables.GetValueOrDefault(SdkConstants.Variables.Build.BuildId)?.Value ?? string.Empty;
            if (!int.TryParse(buildIdStr, out int buildId))
            {
                throw new ArgumentException($"Run Id is not an Int32: {buildIdStr}");
            }

            context.Output($"Downloading artifact '{artifactName}' to: '{targetPath}'");

            // Definition ID is a dummy value only used by HTTP client routing purposes
            int definitionId = 1;

            var pipelinesHelper = new PipelinesServer(context.VssConnection);

            var actionsStorageArtifact = await pipelinesHelper.GetActionsStorageArtifact(definitionId, buildId, artifactName, token);

            if (actionsStorageArtifact == null)
            {
                throw new Exception($"The actions storage artifact for '{artifactName}' could not be found, or is no longer available");
            }

            string containerPath = actionsStorageArtifact.Name; // In actions storage artifacts, name equals the path
            long containerId = actionsStorageArtifact.ContainerId;

            FileContainerServer fileContainerServer = new FileContainerServer(context.VssConnection, projectId: new Guid(), containerId, containerPath);
            await fileContainerServer.DownloadFromContainerAsync(context, targetPath, token);

            context.Output("Artifact download finished.");
        }
    }
}
