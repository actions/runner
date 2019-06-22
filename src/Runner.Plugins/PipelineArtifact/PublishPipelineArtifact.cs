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
            public static readonly string TargetPath = "path";
        }

        private static readonly Regex jobIdentifierRgx = new Regex("[^a-zA-Z0-9 - .]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public async Task RunAsync(
            RunnerActionPluginExecutionContext context,
            CancellationToken token)
        {
            string artifactName = context.GetInput(ArtifactEventProperties.ArtifactName, required: false);
            string targetPath = context.GetInput(ArtifactEventProperties.TargetPath, required: true);
            string defaultWorkingDirectory = context.GetGitHubContext("workspace");

            targetPath = Path.IsPathFullyQualified(targetPath) ? targetPath : Path.GetFullPath(Path.Combine(defaultWorkingDirectory, targetPath));

            string hostType = context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.HostType)?.Value;
            if (!string.Equals(hostType, "Build", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    StringUtil.Loc("CannotUploadFromCurrentEnvironment", hostType ?? string.Empty));
            }

            if (String.IsNullOrWhiteSpace(artifactName))
            {
                string jobIdentifier = context.Variables.GetValueOrDefault(WellKnownDistributedTaskVariables.JobIdentifier).Value;
                var normalizedJobIdentifier = NormalizeJobIdentifier(jobIdentifier);
                artifactName = normalizedJobIdentifier;
            }

            if (!PipelineArtifactPathHelper.IsValidArtifactName(artifactName))
            {
                throw new ArgumentException(StringUtil.Loc("ArtifactNameIsNotValid", artifactName));
            }

            // Project ID
            Guid projectId = new Guid(context.Variables.GetValueOrDefault(BuildVariables.TeamProjectId)?.Value ?? Guid.Empty.ToString());
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            // Build ID
            string buildIdStr = context.Variables.GetValueOrDefault(BuildVariables.BuildId)?.Value ?? string.Empty;
            if (!int.TryParse(buildIdStr, out int buildId))
            {
                // This should not happen since the build id comes from build environment. But a user may override that so we must be careful.
                throw new ArgumentException(StringUtil.Loc("BuildIdIsNotValid", buildIdStr));
            }

            string fullPath = Path.GetFullPath(targetPath);
            bool isFile = File.Exists(fullPath);
            bool isDir = Directory.Exists(fullPath);
            if (!isFile && !isDir)
            {
                // if local path is neither file nor folder
                throw new FileNotFoundException(StringUtil.Loc("PathDoesNotExist", targetPath));
            }

            // Upload to VSTS BlobStore, and associate the artifact with the build.
            context.Output(StringUtil.Loc("UploadingPipelineArtifact", fullPath, buildId));
            PipelineArtifactServer server = new PipelineArtifactServer();
            await server.UploadAsync(context, projectId, buildId, artifactName, fullPath, token);
            context.Output(StringUtil.Loc("UploadArtifactFinished"));
        }

        private string NormalizeJobIdentifier(string jobIdentifier)
        {
            jobIdentifier = jobIdentifierRgx.Replace(jobIdentifier, string.Empty).Replace(".default", string.Empty);
            return jobIdentifier;
        }
    }
}