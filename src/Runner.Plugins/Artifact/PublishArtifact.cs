using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Services.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Plugins.Artifact
{
    public class PublishArtifact : IRunnerActionPlugin
    {
        private static class PublishArtifactInputNames
        {
            public static readonly string ArtifactName = "artifactName";
            public static readonly string Name = "name";
            public static readonly string Path = "path";
        }

        public async Task RunAsync(
            RunnerActionPluginExecutionContext context,
            CancellationToken token)
        {
            string artifactName = context.GetInput(PublishArtifactInputNames.ArtifactName, required: false);  // Back compat since we rename input `artifactName` to `name`
            if (string.IsNullOrEmpty(artifactName))
            {
                artifactName = context.GetInput(PublishArtifactInputNames.Name, required: true);
            }

            string targetPath = context.GetInput(PublishArtifactInputNames.Path, required: true);
            string defaultWorkingDirectory = context.GetGitHubContext("workspace");

            targetPath = Path.IsPathFullyQualified(targetPath) ? targetPath : Path.GetFullPath(Path.Combine(defaultWorkingDirectory, targetPath));

            if (String.IsNullOrWhiteSpace(artifactName))
            {
                throw new ArgumentException($"Artifact name can not be empty string");
            }

            if (Path.GetInvalidFileNameChars().Any(x => artifactName.Contains(x)))
            {
                throw new ArgumentException($"Artifact name is not valid: {artifactName}. It cannot contain '\\', '/', \"', ':', '<', '>', '|', '*', and '?'");
            }

            // Build ID
            string buildIdStr = context.Variables.GetValueOrDefault(SdkConstants.Variables.Build.BuildId)?.Value ?? string.Empty;
            if (!int.TryParse(buildIdStr, out int buildId))
            {
                throw new ArgumentException($"Run Id is not an Int32: {buildIdStr}");
            }

            string fullPath = Path.GetFullPath(targetPath);
            bool isFile = File.Exists(fullPath);
            bool isDir = Directory.Exists(fullPath);
            if (!isFile && !isDir)
            {
                // if local path is neither file nor folder
                throw new FileNotFoundException($"Path does not exist {targetPath}");
            }

            // Container ID
            string containerIdStr = context.Variables.GetValueOrDefault(SdkConstants.Variables.Build.ContainerId)?.Value ?? string.Empty;
            if (!long.TryParse(containerIdStr, out long containerId))
            {
                throw new ArgumentException($"Container Id is not an Int64: {containerIdStr}");
            }

            context.Output($"Uploading artifact '{artifactName}' from '{fullPath}' for run #{buildId}");

            FileContainerServer fileContainerHelper = new FileContainerServer(context.VssConnection, projectId: Guid.Empty, containerId, artifactName);
            var propertiesDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            long size = 0;

            try
            {
                size = await fileContainerHelper.CopyToContainerAsync(context, fullPath, token);

                propertiesDictionary.Add("artifactsize", size.ToString());

                context.Output($"Uploaded '{size}' bytes from '{fullPath}' to server");
            }
            // if any of the results were successful, make sure to attach them to the build
            finally
            {
                // Definition ID is a dummy value only used by HTTP client routing purposes
                int definitionId = 1;

                PipelinesServer pipelinesHelper = new PipelinesServer(context.VssConnection);

                var artifact = await pipelinesHelper.AssociateActionsStorageArtifactAsync(
                    definitionId,
                    buildId,
                    containerId,
                    artifactName,
                    size,
                    token);

                context.Output($"Associated artifact {artifactName} ({artifact.ContainerId}) with run #{buildId}"); 
            }
        }
    }
}