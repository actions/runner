using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Build.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Plugins.PipelineArtifact
{
    // Can be invoked from a build run or a release run should a build be set as the artifact. 
    public class DownloadPipelineArtifact : IRunnerActionPlugin
    {
        static readonly string sourceRunCurrent = "current";
        static readonly string sourceRunSpecific = "specific";
        static readonly string pipelineVersionToDownloadLatest = "latest";
        static readonly string pipelineVersionToDownloadSpecific = "specific";
        static readonly string pipelineVersionToDownloadLatestFromBranch = "latestFromBranch";

        private string DownloadPath => "path";
        private string pipelineRunId => "runId";

        // Properties set by tasks
        private static class ArtifactEventProperties
        {
            public static readonly string SourceRun = "source";
            public static readonly string Project = "project";
            public static readonly string PipelineDefinition = "pipeline";
            public static readonly string PipelineTriggering = "preferTriggeringPipeline";
            public static readonly string PipelineVersionToDownload = "runVersion";
            public static readonly string BranchName = "runBranch";
            public static readonly string Tags = "tags";
            public static readonly string ArtifactName = "name";
            public static readonly string ItemPattern = "patterns";
        }

        public async Task RunAsync(
            RunnerActionPluginExecutionContext context,
            CancellationToken token)
        {
            ArgUtil.NotNull(context, nameof(context));
            string artifactName = context.GetInput(ArtifactEventProperties.ArtifactName, required: true);
            string branchName = context.GetInput(ArtifactEventProperties.BranchName, required: false);
            string pipelineDefinition = context.GetInput(ArtifactEventProperties.PipelineDefinition, required: false);
            string sourceRun = context.GetInput(ArtifactEventProperties.SourceRun, required: false);
            string pipelineTriggering = context.GetInput(ArtifactEventProperties.PipelineTriggering, required: false);
            string pipelineVersionToDownload = context.GetInput(ArtifactEventProperties.PipelineVersionToDownload, required: false);
            string targetPath = context.GetInput(DownloadPath, required: false);
            string environmentBuildId = context.Variables.GetValueOrDefault(BuildVariables.BuildId)?.Value ?? string.Empty; // BuildID provided by environment.
            string itemPattern = context.GetInput(ArtifactEventProperties.ItemPattern, required: false) ?? string.Empty;
            string projectName = context.GetInput(ArtifactEventProperties.Project, required: false);
            string tags = context.GetInput(ArtifactEventProperties.Tags, required: false) ?? string.Empty;
            string userSpecifiedpipelineId = context.GetInput(pipelineRunId, required: false);
            string defaultWorkingDirectory = context.GetGitHubContext("workspace");

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = artifactName;
            }

            targetPath = Path.IsPathFullyQualified(targetPath) ? targetPath : Path.GetFullPath(Path.Combine(defaultWorkingDirectory, targetPath));

            if (!PipelineArtifactPathHelper.IsValidArtifactName(artifactName))
            {
                throw new ArgumentException($"Artifact name is not valid: {artifactName}. It cannot contain '\\', /', \"', ':', '<', '>', '|', '*', and '?'");
            }

            string[] minimatchPatterns = itemPattern.Split(
                new[] { "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );

            string[] tagsInput = tags.Split(
                new[] { "," },
                StringSplitOptions.None
            );

            PipelineArtifactServer server = new PipelineArtifactServer();
            PipelineArtifactDownloadParameters downloadParameters;

            // TODO: use a constant for project id, which is currently defined in GitHub.Services.Agent.Constants.Variables.System.TeamProjectId (Ting)
            string projectIdStr = context.Variables.GetValueOrDefault("system.teamProjectId")?.Value;
            if (String.IsNullOrEmpty(projectIdStr))
            {
                throw new ArgumentNullException("Project ID cannot be null.");
            }
            Guid projectId = Guid.Parse(projectIdStr);
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            int pipelineId = 0;
            if (int.TryParse(environmentBuildId, out pipelineId) && pipelineId != 0)
            {
                context.Output($"Download from the specified build: #{pipelineId}");
            }
            else
            {
                // This should not happen since the build id comes from build environment. But a user may override that so we must be careful.
                throw new ArgumentException($"Build Id is not valid: {environmentBuildId}");
            }

            downloadParameters = new PipelineArtifactDownloadParameters
            {
                ProjectRetrievalOptions = BuildArtifactRetrievalOptions.RetrieveByProjectId,
                ProjectId = projectId,
                PipelineId = pipelineId,
                ArtifactName = artifactName,
                TargetDirectory = targetPath,
                MinimatchFilters = minimatchPatterns,
                MinimatchFilterWithArtifactName = true
            };

            string fullPath = this.CreateDirectoryIfDoesntExist(targetPath);

            DownloadOptions downloadOptions;
            if (string.IsNullOrEmpty(downloadParameters.ArtifactName))
            {
                downloadOptions = DownloadOptions.MultiDownload;
            }
            else
            {
                downloadOptions = DownloadOptions.SingleDownload;
            }

            context.Output($"Download artifact to: {targetPath}");
            await server.DownloadAsyncV2(context, downloadParameters, downloadOptions, token);
            context.Output("Downloading artifact finished.");
        }

        private string CreateDirectoryIfDoesntExist(string targetPath)
        {
            string fullPath = Path.GetFullPath(targetPath);
            bool dirExists = Directory.Exists(fullPath);
            if (!dirExists)
            {
                Directory.CreateDirectory(fullPath);
            }
            return fullPath;
        }

        private async Task<int> GetPipelineIdAsync(RunnerActionPluginExecutionContext context, string pipelineDefinition, string pipelineVersionToDownload, string project, string[] tagFilters, string branchName = null)
        {
            var definitions = new List<int>() { Int32.Parse(pipelineDefinition) };
            VssConnection connection = context.VssConnection;
            BuildHttpClient buildHttpClient = connection.GetClient<BuildHttpClient>();
            List<GitHub.Build.WebApi.Build> list;
            if (pipelineVersionToDownload == "latest")
            {
                list = await buildHttpClient.GetBuildsAsync(project, definitions, tagFilters: tagFilters, queryOrder: BuildQueryOrder.FinishTimeDescending);
            }
            else if (pipelineVersionToDownload == "latestFromBranch")
            {
                list = await buildHttpClient.GetBuildsAsync(project, definitions, branchName: branchName, tagFilters: tagFilters, queryOrder: BuildQueryOrder.FinishTimeDescending);
            }
            else
            {
                throw new InvalidOperationException("Unreachable code!");
            }

            if (list.Count > 0)
            {
                return list.First().Id;
            }
            else
            {
                throw new ArgumentException("No builds currently exist in the build definition supplied.");
            }
        }
    }
}
