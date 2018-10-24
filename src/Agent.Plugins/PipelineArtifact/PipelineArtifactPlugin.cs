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
    public abstract class PipelineArtifactTaskPluginBase : IAgentTaskPlugin
    {
        public abstract Guid Id { get; }
        public string Version => "0.139.0"; // Publish and Download tasks will be always on the same version.
        public string Stage => "main";

        public async Task RunAsync(AgentTaskPluginExecutionContext context, CancellationToken token)
        {
            ArgUtil.NotNull(context, nameof(context));

            // Artifact Name
            string artifactName = context.GetInput(ArtifactEventProperties.ArtifactName, required: true);

            // Path
            // TODO: Translate targetPath from container to host (Ting)
            string targetPath = context.GetInput(ArtifactEventProperties.TargetPath, required: true);

            await ProcessCommandInternalAsync(context, targetPath, artifactName, token);
        }

        // Process the command with preprocessed arguments.
        protected abstract Task ProcessCommandInternalAsync(
            AgentTaskPluginExecutionContext context, 
            string targetPath, 
            string artifactName, 
            CancellationToken token);
    }

    // Caller: PublishPipelineArtifact task
    // Can be invoked from a build run or a release run should a build be set as the artifact. 
    public class PublishPipelineArtifactTask : PipelineArtifactTaskPluginBase
    {
        // Same as: https://github.com/Microsoft/vsts-tasks/blob/master/Tasks/PublishPipelineArtifactV0/task.json
        public override Guid Id => new Guid("ECDC45F6-832D-4AD9-B52B-EE49E94659BE");

        protected override async Task ProcessCommandInternalAsync(
            AgentTaskPluginExecutionContext context, 
            string targetPath, 
            string artifactName,
            CancellationToken token)
        {
            string hostType = context.Variables.GetValueOrDefault("system.hosttype")?.Value; 
            if (!string.Equals(hostType, "Build", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException(
                    StringUtil.Loc("CannotUploadFromCurrentEnvironment", hostType ?? string.Empty)); 
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
                throw new FileNotFoundException(StringUtil.Loc("PathNotExist", targetPath));
            }

            // Upload to VSTS BlobStore, and associate the artifact with the build.
            context.Output(StringUtil.Loc("UploadingPipelineArtifact", fullPath, buildId));
            PipelineArtifactServer server = new PipelineArtifactServer();
            await server.UploadAsync(context, projectId, buildId, artifactName, fullPath, token);
            context.Output(StringUtil.Loc("UploadArtifactFinished"));
        }
    }

    // CAller: DownloadPipelineArtifact task
    // Can be invoked from a build run or a release run should a build be set as the artifact. 
    public class DownloadPipelineArtifactTask : PipelineArtifactTaskPluginBase
    {
        // Same as https://github.com/Microsoft/vsts-tasks/blob/master/Tasks/DownloadPipelineArtifactV0/task.json
        public override Guid Id => new Guid("61F2A582-95AE-4948-B34D-A1B3C4F6A737");

        protected override async Task ProcessCommandInternalAsync(
            AgentTaskPluginExecutionContext context, 
            string targetPath, 
            string artifactName,
            CancellationToken token)
        {
            // Create target directory if absent
            string fullPath = Path.GetFullPath(targetPath);
            bool isDir = Directory.Exists(fullPath);
            if (!isDir)
            {
                Directory.CreateDirectory(fullPath);
            }

            // Project ID
            // TODO: use a constant for project id, which is currently defined in Microsoft.VisualStudio.Services.Agent.Constants.Variables.System.TeamProjectId (Ting)
            string guidStr = context.Variables.GetValueOrDefault("system.teamProjectId")?.Value;
            Guid.TryParse(guidStr, out Guid projectId);
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            // Build ID
            int buildId = 0;
            string buildIdStr = context.GetInput(ArtifactEventProperties.PipelineId, required: false);
            // Determine the build id
            if (Int32.TryParse(buildIdStr, out buildId) && buildId != 0)
            {
                // A) Build Id provided by user input
                context.Output(StringUtil.Loc("DownloadingFromBuild", buildId));
            }
            else
            {
                // B) Build Id provided by environment
                buildIdStr = context.Variables.GetValueOrDefault(BuildVariables.BuildId)?.Value ?? string.Empty;
                if (int.TryParse(buildIdStr, out buildId) && buildId != 0)
                {
                    context.Output(StringUtil.Loc("DownloadingFromBuild", buildId));
                }
                else
                {
                    string hostType = context.Variables.GetValueOrDefault("system.hosttype")?.Value; 
                    if (string.Equals(hostType, "Release", StringComparison.OrdinalIgnoreCase) || 
                        string.Equals(hostType, "DeploymentGroup", StringComparison.OrdinalIgnoreCase)) {
                        throw new InvalidOperationException(StringUtil.Loc("BuildIdIsNotAvailable", hostType ?? string.Empty)); 
                    } else if (!string.Equals(hostType, "Build", StringComparison.OrdinalIgnoreCase)) {
                        throw new InvalidOperationException(StringUtil.Loc("CannotDownloadFromCurrentEnvironment", hostType ?? string.Empty));
                    } else {
                        // This should not happen since the build id comes from build environment. But a user may override that so we must be careful.
                        throw new ArgumentException(StringUtil.Loc("BuildIdIsNotValid", buildIdStr));
                    }
                }
            }

            // Download from VSTS BlobStore
            context.Output(StringUtil.Loc("DownloadArtifactTo", targetPath));
            PipelineArtifactServer server = new PipelineArtifactServer();
            await server.DownloadAsync(context, projectId, buildId, artifactName, targetPath, token);
            context.Output(StringUtil.Loc("DownloadArtifactFinished"));
        }
    }

    // Properties set by tasks
    internal static class ArtifactEventProperties
    {
        public static readonly string ArtifactName = "artifactName";
        public static readonly string TargetPath = "targetPath";
        public static readonly string PipelineId = "pipelineId";
    }
}