using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.BlobStore.Common;
using Microsoft.VisualStudio.Services.Content.Common.Tracing;
using Microsoft.VisualStudio.Services.BlobStore.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Agent.Sdk;

namespace Agent.Plugins.PipelineArtifact
{
    public abstract class PipelineArtifactTaskPluginBaseV1 : IAgentTaskPlugin
    {
        public abstract Guid Id { get; }
        public virtual string Version => "1.0.0"; // Publish and Download tasks will be always on the same version.
        protected virtual string TargetPath => "targetPath";
        protected virtual string PipelineId => "pipelineId";
        public string Stage => "main";

        public Task RunAsync(AgentTaskPluginExecutionContext context, CancellationToken token)
        {
            return this.ProcessCommandInternalAsync(context, token);
        }

        protected abstract Task ProcessCommandInternalAsync(
            AgentTaskPluginExecutionContext context, 
            CancellationToken token);

        // Properties set by tasks
        protected static class ArtifactEventProperties
        {
            public static readonly string BuildType = "buildType";
            public static readonly string Project = "project";
            public static readonly string BuildPipelineDefinition = "definition";
            public static readonly string BuildTriggering = "specificBuildWithTriggering";
            public static readonly string BuildVersionToDownload = "buildVersionToDownload";
            public static readonly string BranchName = "branchName";
            public static readonly string Tags = "tags";
            public static readonly string ArtifactName = "artifactName";
            public static readonly string ItemPattern = "itemPattern";
        }
    }

    // Can be invoked from a build run or a release run should a build be set as the artifact. 
    public class DownloadPipelineArtifactTaskV1 : PipelineArtifactTaskPluginBaseV1
    {
        // Same as https://github.com/Microsoft/vsts-tasks/blob/master/Tasks/DownloadPipelineArtifactV1/task.json
        public override Guid Id => PipelineArtifactPluginConstants.DownloadPipelineArtifactTaskId;
        static readonly string buildTypeCurrent = "current";
        static readonly string buildTypeSpecific = "specific";
        static readonly string buildVersionToDownloadLatest = "latest";
        static readonly string buildVersionToDownloadSpecific = "specific";
        static readonly string buildVersionToDownloadLatestFromBranch = "latestFromBranch";

        protected override async Task ProcessCommandInternalAsync(
            AgentTaskPluginExecutionContext context, 
            CancellationToken token)
        {
            ArgUtil.NotNull(context, nameof(context));
            string artifactName = this.GetArtifactName(context);
            string branchName = context.GetInput(ArtifactEventProperties.BranchName, required: false);
            string buildPipelineDefinition = context.GetInput(ArtifactEventProperties.BuildPipelineDefinition, required: false);
            string buildType = context.GetInput(ArtifactEventProperties.BuildType, required: true);
            string buildTriggering = context.GetInput(ArtifactEventProperties.BuildTriggering, required: false);
            string buildVersionToDownload = context.GetInput(ArtifactEventProperties.BuildVersionToDownload, required: false);
            string targetPath = context.GetInput(TargetPath, required: true);
            string environmentBuildId = context.Variables.GetValueOrDefault(BuildVariables.BuildId)?.Value ?? string.Empty; // BuildID provided by environment.
            string itemPattern = context.GetInput(ArtifactEventProperties.ItemPattern, required: false);
            string projectName = context.GetInput(ArtifactEventProperties.Project, required: false);
            string tags = context.GetInput(ArtifactEventProperties.Tags, required: false);
            string userSpecifiedpipelineId = context.GetInput(PipelineId, required: false);

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
            if (buildType == buildTypeCurrent)
            {
                // TODO: use a constant for project id, which is currently defined in Microsoft.VisualStudio.Services.Agent.Constants.Variables.System.TeamProjectId (Ting)
                string projectIdStr = context.Variables.GetValueOrDefault("system.teamProjectId")?.Value;
                if(String.IsNullOrEmpty(projectIdStr))
                {
                    throw new ArgumentNullException("Project ID cannot be null.");
                }
                Guid projectId = Guid.Parse(projectIdStr);
                ArgUtil.NotEmpty(projectId, nameof(projectId));

                int pipelineId = 0;
                if (int.TryParse(environmentBuildId, out pipelineId) && pipelineId != 0)
                {
                    context.Output(StringUtil.Loc("DownloadingFromBuild", pipelineId));
                }
                else
                {
                    string hostType = context.Variables.GetValueOrDefault("system.hosttype")?.Value;
                    if (string.Equals(hostType, "Release", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(hostType, "DeploymentGroup", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(StringUtil.Loc("BuildIdIsNotAvailable", hostType ?? string.Empty));
                    }
                    else if (!string.Equals(hostType, "Build", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(StringUtil.Loc("CannotDownloadFromCurrentEnvironment", hostType ?? string.Empty));
                    }
                    else
                    {
                        // This should not happen since the build id comes from build environment. But a user may override that so we must be careful.
                        throw new ArgumentException(StringUtil.Loc("BuildIdIsNotValid", environmentBuildId));
                    }
                }
                downloadParameters = new PipelineArtifactDownloadParameters
                {
                    ProjectRetrievalOptions = BuildArtifactRetrievalOptions.RetrieveByProjectId,
                    ProjectId = projectId,
                    PipelineId = pipelineId,
                    ArtifactName = artifactName,
                    TargetDirectory = targetPath,
                    MinimatchFilters = minimatchPatterns
                };
            }
            else if (buildType == buildTypeSpecific)
            {
                int pipelineId;
                if (buildVersionToDownload == buildVersionToDownloadLatest)
                {
                    pipelineId = await this.GetpipelineIdAsync(context, buildPipelineDefinition, buildVersionToDownload, projectName, tagsInput);
                }
                else if (buildVersionToDownload == buildVersionToDownloadSpecific)
                {
                    pipelineId = Int32.Parse(userSpecifiedpipelineId);
                }
                else if (buildVersionToDownload == buildVersionToDownloadLatestFromBranch)
                {
                    pipelineId = await this.GetpipelineIdAsync(context, buildPipelineDefinition, buildVersionToDownload, projectName, tagsInput, branchName);
                }
                else
                {
                    throw new InvalidOperationException("Unreachable code!");
                }
                downloadParameters = new PipelineArtifactDownloadParameters
                {
                    ProjectRetrievalOptions = BuildArtifactRetrievalOptions.RetrieveByProjectName,
                    ProjectName = projectName,
                    PipelineId = pipelineId,
                    ArtifactName = artifactName,
                    TargetDirectory = targetPath,
                    MinimatchFilters = minimatchPatterns
                };
            }
            else
            {
                throw new InvalidOperationException($"Build type '{buildType}' is not recognized.");
            }

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

            context.Output(StringUtil.Loc("DownloadArtifactTo", targetPath));
            await server.DownloadAsync(context, downloadParameters, downloadOptions, token);
            context.Output(StringUtil.Loc("DownloadArtifactFinished"));
        }

        protected virtual string GetArtifactName(AgentTaskPluginExecutionContext context)
        {
            return context.GetInput(ArtifactEventProperties.ArtifactName, required: true);
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

        private async Task<int> GetpipelineIdAsync(AgentTaskPluginExecutionContext context, string buildPipelineDefinition, string buildVersionToDownload, string project, string[] tagFilters, string branchName=null)
        {
            var definitions = new List<int>() { Int32.Parse(buildPipelineDefinition) };
            VssConnection connection = context.VssConnection;
            BuildHttpClient buildHttpClient = connection.GetClient<BuildHttpClient>();
            List<Build> list;
            if (buildVersionToDownload == "latest")
            {
                list = await buildHttpClient.GetBuildsAsync(project, definitions, tagFilters: tagFilters, queryOrder: BuildQueryOrder.FinishTimeDescending);
            }
            else if (buildVersionToDownload == "latestFromBranch")
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

    public class DownloadPipelineArtifactTaskV1_1_0 : DownloadPipelineArtifactTaskV1
    {
        public override string Version => "1.1.0";
        protected override string TargetPath => "downloadPath";
        protected override string PipelineId => "buildId";

        protected override string GetArtifactName(AgentTaskPluginExecutionContext context)
        {
            return context.GetInput(ArtifactEventProperties.ArtifactName, required: false);
        }
    }

    public class DownloadPipelineArtifactTaskV1_1_1 : DownloadPipelineArtifactTaskV1
    {
        public override string Version => "1.1.1";

        protected override string GetArtifactName(AgentTaskPluginExecutionContext context)
        {
            return context.GetInput(ArtifactEventProperties.ArtifactName, required: false);
        }
    }

    // 1.1.2 is the same as 1.1.0 because we reverted 1.1.1 change.
    public class DownloadPipelineArtifactTaskV1_1_2 : DownloadPipelineArtifactTaskV1_1_0
    {
        public override string Version => "1.1.2";
    }

    // 1.1.3 is the same as 1.1.0 because we reverted 1.1.1 change and the minimum agent version.
    public class DownloadPipelineArtifactTaskV1_1_3 : DownloadPipelineArtifactTaskV1_1_0
    {
        public override string Version => "1.1.3";
    }
}