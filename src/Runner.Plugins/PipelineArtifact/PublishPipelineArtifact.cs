using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Build.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Sdk;
using System.Text.RegularExpressions;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    // Caller: PublishPipelineArtifact task
    // Can be invoked from a build run or a release run should a build be set as the artifact. 
    public class PublishPipelineArtifact : IRunnerActionPlugin
    {
        // Properties set by tasks
        private static class ArtifactEventProperties
        {
            public static readonly string ArtifactName = "artifactName";
            public static readonly string Name = "name";
            public static readonly string TargetPath = "path";
        }

        private static readonly Regex jobIdentifierRgx = new Regex("[^a-zA-Z0-9 - .]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public async Task RunAsync(
            RunnerActionPluginExecutionContext context,
            CancellationToken token)
        {
            string artifactName = context.GetInput(ArtifactEventProperties.ArtifactName, required: false);  // Back compat since we rename input `artifactName` to `name`
            if (string.IsNullOrEmpty(artifactName))
            {
                artifactName = context.GetInput(ArtifactEventProperties.Name, required: true);
            }
            string targetPath = context.GetInput(ArtifactEventProperties.TargetPath, required: true);
            string artifactService = context.GetInput("as");
            string defaultWorkingDirectory = context.GetGitHubContext("workspace");

            targetPath = Path.IsPathFullyQualified(targetPath) ? targetPath : Path.GetFullPath(Path.Combine(defaultWorkingDirectory, targetPath));

            string hostType = context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.HostType)?.Value;
            if (!string.Equals(hostType, "Build", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Cannot upload to a pipeline artifact from {hostType ?? string.Empty} environment.");
            }

            if (String.IsNullOrWhiteSpace(artifactName))
            {
                string jobIdentifier = context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.JobIdentifier).Value;
                var normalizedJobIdentifier = NormalizeJobIdentifier(jobIdentifier);
                artifactName = normalizedJobIdentifier;
            }

            if (!PipelineArtifactPathHelper.IsValidArtifactName(artifactName))
            {
                throw new ArgumentException($"Artifact name is not valid: {artifactName}. It cannot contain '\\', /', \"', ':', '<', '>', '|', '*', and '?'");
            }

            // Project ID
            Guid projectId = new Guid(context.Variables.GetValueOrDefault(BuildVariables.TeamProjectId)?.Value ?? Guid.Empty.ToString());
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            // Build ID
            string buildIdStr = context.Variables.GetValueOrDefault(BuildVariables.BuildId)?.Value ?? string.Empty;
            if (!int.TryParse(buildIdStr, out int buildId))
            {
                // This should not happen since the build id comes from build environment. But a user may override that so we must be careful.
                throw new ArgumentException($"Build Id is not valid: {buildIdStr}");
            }

            string fullPath = Path.GetFullPath(targetPath);
            bool isFile = File.Exists(fullPath);
            bool isDir = Directory.Exists(fullPath);
            if (!isFile && !isDir)
            {
                // if local path is neither file nor folder
                throw new FileNotFoundException($"Path does not exist {targetPath}");
            }

            if (!string.IsNullOrEmpty(artifactService))
            {
                // Upload to BlobStore, and associate the artifact with the build.
                context.Output($"Uploading pipeline artifact from {fullPath} for build #{buildId}");
                PipelineArtifactServer server = new PipelineArtifactServer();
                await server.UploadAsync(context, projectId, buildId, artifactName, fullPath, token);
                context.Output("Uploading pipeline artifact finished.");
            }
            else
            {
                // Container ID
                string containerIdStr = context.Variables.GetValueOrDefault(BuildVariables.ContainerId)?.Value ?? string.Empty;
                if (!long.TryParse(containerIdStr, out long containerId))
                {
                    throw new ArgumentException($"Container Id is not valid: {containerIdStr}");
                }

                string jobId = context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.JobId).Value ?? string.Empty;

                context.Output($"Uploading artifact from {fullPath} for run #{buildId}");

                FileContainerServer fileContainerHelper = new FileContainerServer(context.VssConnection, projectId, containerId, artifactName);
                long size = await fileContainerHelper.CopyToContainerAsync(context, fullPath, token);
                var propertiesDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                propertiesDictionary.Add("artifactsize", size.ToString());

                string fileContainerFullPath = StringUtil.Format($"#/{containerId}/{artifactName}");
                context.Output($"Uploaded '{fullPath}' to server");

                BuildServer buildHelper = new BuildServer(context.VssConnection);
                var artifact = await buildHelper.AssociateArtifact(projectId, buildId, jobId, artifactName, ArtifactResourceTypes.Container, fileContainerFullPath, propertiesDictionary, token);
                context.Output($"Associated artifact {artifact.Id} with run {buildId}");
            }
        }

        private string NormalizeJobIdentifier(string jobIdentifier)
        {
            jobIdentifier = jobIdentifierRgx.Replace(jobIdentifier, string.Empty).Replace(".default", string.Empty);
            return jobIdentifier;
        }
    }
}