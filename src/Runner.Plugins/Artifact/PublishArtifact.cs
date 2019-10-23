using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Build.WebApi;
using GitHub.Services.Common;
using GitHub.DistributedTask.WebApi;
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

            // Project ID
            Guid projectId = new Guid(context.Variables.GetValueOrDefault(BuildVariables.TeamProjectId)?.Value ?? Guid.Empty.ToString());

            // Build ID
            string buildIdStr = context.Variables.GetValueOrDefault(BuildVariables.BuildId)?.Value ?? string.Empty;
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
            string containerIdStr = context.Variables.GetValueOrDefault(BuildVariables.ContainerId)?.Value ?? string.Empty;
            if (!long.TryParse(containerIdStr, out long containerId))
            {
                throw new ArgumentException($"Container Id is not a Int64: {containerIdStr}");
            }

            context.Output($"Uploading artifact '{artifactName}' from '{fullPath}' for run #{buildId}");

            FileContainerServer fileContainerHelper = new FileContainerServer(context.VssConnection, projectId, containerId, artifactName);
            var propertiesDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                long size = await fileContainerHelper.CopyToContainerAsync(context, fullPath, token);
                propertiesDictionary.Add("artifactsize", size.ToString());
                context.Output($"Uploaded '{size}' bytes from '{fullPath}' to server");
            }
            // if any of the results were successful, make sure to attach them to the build
            finally
            {
                string fileContainerFullPath = StringUtil.Format($"#/{containerId}/{artifactName}");
                BuildServer buildHelper = new BuildServer(context.VssConnection);
                string jobId = context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.JobId).Value ?? string.Empty;
                var artifact = await buildHelper.AssociateArtifact(projectId, buildId, jobId, artifactName, ArtifactResourceTypes.Container, fileContainerFullPath, propertiesDictionary, token);
                context.Output($"Associated artifact {artifactName} ({artifact.Id}) with run #{buildId}");
            }
        }
    }
}