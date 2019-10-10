using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Build.WebApi;
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

            // Project ID
            Guid projectId = new Guid(context.Variables.GetValueOrDefault(BuildVariables.TeamProjectId)?.Value ?? Guid.Empty.ToString());

            // Build ID
            string buildIdStr = context.Variables.GetValueOrDefault(BuildVariables.BuildId)?.Value ?? string.Empty;
            if (!int.TryParse(buildIdStr, out int buildId))
            {
                throw new ArgumentException($"Run Id is not an Int32: {buildIdStr}");
            }

            context.Output($"Download artifact '{artifactName}' to: '{targetPath}'");

            BuildServer buildHelper = new BuildServer(context.VssConnection);
            BuildArtifact buildArtifact = await buildHelper.GetArtifact(projectId, buildId, artifactName, token);

            if (string.Equals(buildArtifact.Resource.Type, "Container", StringComparison.OrdinalIgnoreCase))
            {
                string containerUrl = buildArtifact.Resource.Data;
                string[] parts = containerUrl.Split(new[] { '/' }, 3);
                if (parts.Length < 3 || !long.TryParse(parts[1], out long containerId))
                {
                    throw new ArgumentOutOfRangeException($"Invalid container url '{containerUrl}' for artifact '{buildArtifact.Name}'");
                }

                string containerPath = parts[2];
                FileContainerServer fileContainerServer = new FileContainerServer(context.VssConnection, projectId, containerId, containerPath);
                await fileContainerServer.DownloadFromContainerAsync(context, targetPath, token);
            }
            else
            {
                throw new NotSupportedException($"Invalid artifact type: {buildArtifact.Resource.Type}");
            }

            context.Output("Artifact download finished.");
        }
    }
}
