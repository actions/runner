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

            // Determine whether to call Pipelines or Build endpoint to publish artifact based on variable setting
            string usePipelinesArtifactEndpointVar = context.Variables.GetValueOrDefault("Runner.UseActionsArtifactsApis")?.Value;
            bool.TryParse(usePipelinesArtifactEndpointVar, out bool usePipelinesArtifactEndpoint);
            string containerPath;
            long containerId;

            context.Output($"Download artifact '{artifactName}' to: '{targetPath}'");

            if (usePipelinesArtifactEndpoint)
            {
                context.Debug("Downloading artifact using Pipelines endpoint");

                // Definition ID
                string definitionIdStr = context.Variables.GetValueOrDefault(BuildVariables.DefinitionId)?.Value ?? string.Empty;

                context.Output($"Definition id: {definitionIdStr}");

                if (!int.TryParse(definitionIdStr, out int definitionId))
                {
                    throw new ArgumentException($"Definition Id is not an Int32: {definitionIdStr}");
                }

                PipelinesServer pipelinesHelper = new PipelinesServer(context.VssConnection);

                var actionsStorageArtifact = await pipelinesHelper.GetActionsStorageArtifact(definitionId, buildId, artifactName, token);

                if (actionsStorageArtifact == null)
                {
                    throw new Exception($"The actions storage artifact for '{artifactName}' could not be found, or is no longer available");
                }

                containerPath = actionsStorageArtifact.Name; // In actions storage artifacts, name equals the path
                containerId = actionsStorageArtifact.ContainerId;
            }
            else
            {
                context.Debug("Downloading artifact using Build2 endpoint");

                BuildServer buildHelper = new BuildServer(context.VssConnection);
                BuildArtifact buildArtifact = await buildHelper.GetArtifact(projectId, buildId, artifactName, token);

                if (string.Equals(buildArtifact.Resource.Type, "Container", StringComparison.OrdinalIgnoreCase) ||
                    // Artifact was published by Pipelines endpoint, check new type here to handle rollback scenario
                    string.Equals(buildArtifact.Resource.Type, "Actions_Storage", StringComparison.OrdinalIgnoreCase))
                {
                    string containerUrl = buildArtifact.Resource.Data;
                    string[] parts = containerUrl.Split(new[] { '/' }, 3);
                    if (parts.Length < 3 || !long.TryParse(parts[1], out containerId))
                    {
                        throw new ArgumentOutOfRangeException($"Invalid container url '{containerUrl}' for artifact '{buildArtifact.Name}'");
                    }

                    containerPath = parts[2];
                }
                else
                {
                    throw new NotSupportedException($"Invalid artifact type: {buildArtifact.Resource.Type}");
                }
            }

            FileContainerServer fileContainerServer = new FileContainerServer(context.VssConnection, projectId, containerId, containerPath);
            await fileContainerServer.DownloadFromContainerAsync(context, targetPath, token);

            context.Output("Artifact download finished.");
        }
    }
}
