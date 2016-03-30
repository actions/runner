using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public class BuildCommands : AgentService, ICommandExtension
    {
        public Type ExtensionType
        {
            get
            {
                return typeof(ICommandExtension);
            }
        }

        public string CommandArea
        {
            get
            {
                return "build";
            }
        }

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            if (string.Equals(command.Event, WellKnownBuildCommand.UploadLog, StringComparison.OrdinalIgnoreCase))
            {
                ProcessBuildUploadLogCommand(context, command.Data);
            }
            else if (string.Equals(command.Event, WellKnownBuildCommand.UploadSummary, StringComparison.OrdinalIgnoreCase))
            {
                ProcessBuildUploadSummaryCommand(context, command.Data);
            }
            else if (string.Equals(command.Event, WellKnownBuildCommand.UpdateBuildNumber, StringComparison.OrdinalIgnoreCase))
            {
                ProcessBuildUpdateBuildNumberCommand(context, command.Data);
            }
            else if (string.Equals(command.Event, WellKnownBuildCommand.AddBuildTag, StringComparison.OrdinalIgnoreCase))
            {
                ProcessBuildAddBuildTagCommand(context, command.Data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("BuildCommandNotFound", command.Event));
            }
        }

        private void ProcessBuildUploadLogCommand(IExecutionContext context, string data)
        {
            if (!string.IsNullOrEmpty(data) && File.Exists(data))
            {
                context.QueueAttachFile(CoreAttachmentType.Log, "CustomToolLog", data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("CustomLogDoesNotExist", data ?? string.Empty));
            }
        }

        // ##VSO[build.uploadsummary] command has been deprecated
        // Leave the implementation on agent for back compat
        private void ProcessBuildUploadSummaryCommand(IExecutionContext context, string data)
        {
            if (!string.IsNullOrEmpty(data) && File.Exists(data))
            {
                var fileName = Path.GetFileName(data);
                context.QueueAttachFile(CoreAttachmentType.Summary, StringUtil.Format($"CustomMarkDownSummary-{fileName}"), data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("CustomMarkDownSummaryDoesNotExist", data ?? string.Empty));
            }
        }

        private void ProcessBuildUpdateBuildNumberCommand(IExecutionContext context, string data)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(context.Endpoints, nameof(context.Endpoints));

            ServiceEndpoint systemConnection = context.Endpoints.FirstOrDefault(e => string.Equals(e.Name, ServiceEndpoints.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(systemConnection, nameof(systemConnection));
            ArgUtil.NotNull(systemConnection.Url, nameof(systemConnection.Url));

            Uri projectUrl = systemConnection.Url;
            VssCredentials projectCredential = ApiUtil.GetVssCredential(systemConnection);

            Guid projectId = context.Variables.System_TeamProjectId ?? Guid.Empty;
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            int? buildId = context.Variables.Build_BuildId;
            ArgUtil.NotNull(buildId, nameof(buildId));

            if (!String.IsNullOrEmpty(data))
            {
                // update build number within Context.
                context.Variables.Set(WellKnownBuildVariables.BuildNumber, data);

                // queue async command task to update build number.
                context.Debug($"Update build number for build: {buildId.Value} to: {data} at backend.");
                var commandContext = HostContext.CreateService<IAsyncCommandContext>();
                commandContext.InitializeCommandContext(context, StringUtil.Loc("UpdateBuildNumber"));
                commandContext.Task = UpdateBuildNumberAsync(commandContext,
                                                             projectUrl,
                                                             projectCredential,
                                                             projectId,
                                                             buildId.Value,
                                                             data,
                                                             context.CancellationToken);

                context.AsyncCommands.Add(commandContext);
            }
            else
            {
                throw new Exception(StringUtil.Loc("BuildNumberRequired"));
            }
        }

        private async Task UpdateBuildNumberAsync(
            IAsyncCommandContext context,
            Uri projectCollection,
            VssCredentials credentials,
            Guid projectId,
            int buildId,
            string buildNumber,
            CancellationToken cancellationToken)
        {
            BuildServer buildServer = new BuildServer(projectCollection, credentials, projectId);
            var build = await buildServer.UpdateBuildNumber(buildId, buildNumber, cancellationToken);
            context.Output(StringUtil.Loc("UpdateBuildNumberForBuild", build.BuildNumber, build.Id));
        }

        private void ProcessBuildAddBuildTagCommand(IExecutionContext context, string data)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(context.Endpoints, nameof(context.Endpoints));

            ServiceEndpoint systemConnection = context.Endpoints.FirstOrDefault(e => string.Equals(e.Name, ServiceEndpoints.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(systemConnection, nameof(systemConnection));
            ArgUtil.NotNull(systemConnection.Url, nameof(systemConnection.Url));

            Uri projectUrl = systemConnection.Url;
            VssCredentials projectCredential = ApiUtil.GetVssCredential(systemConnection);

            Guid projectId = context.Variables.System_TeamProjectId ?? Guid.Empty;
            ArgUtil.NotEmpty(projectId, nameof(projectId));

            int? buildId = context.Variables.Build_BuildId;
            ArgUtil.NotNull(buildId, nameof(buildId));

            if (!string.IsNullOrEmpty(data))
            {
                // queue async command task to associate artifact.
                context.Debug($"Add build tag: {data} to build: {buildId.Value} at backend.");
                var commandContext = HostContext.CreateService<IAsyncCommandContext>();
                commandContext.InitializeCommandContext(context, StringUtil.Loc("AddBuildTag"));
                commandContext.Task = AddBuildTagAsync(commandContext,
                                                       projectUrl,
                                                       projectCredential,
                                                       projectId,
                                                       buildId.Value,
                                                       data,
                                                       context.CancellationToken);
                context.AsyncCommands.Add(commandContext);
            }
            else
            {
                throw new Exception(StringUtil.Loc("BuildTagRequired"));
            }
        }

        private async Task AddBuildTagAsync(
            IAsyncCommandContext context,
            Uri projectCollection,
            VssCredentials credentials,
            Guid projectId,
            int buildId,
            string buildTag,
            CancellationToken cancellationToken)
        {
            BuildServer buildServer = new BuildServer(projectCollection, credentials, projectId);
            var tags = await buildServer.AddBuildTag(buildId, buildTag, cancellationToken);

            if (tags == null || !tags.Contains(buildTag))
            {
                throw new Exception(StringUtil.Loc("BuildTagAddFailed", buildTag));
            }
            else
            {
                context.Output(StringUtil.Loc("BuildTagsForBuild", buildId, String.Join(", ", tags)));
            }
        }
    }

    internal static class WellKnownBuildCommand
    {
        public static readonly string UploadLog = "uploadlog";
        public static readonly string UploadSummary = "uploadsummary";
        public static readonly string UpdateBuildNumber = "updatebuildnumber";
        public static readonly string AddBuildTag = "addbuildtag";
    }
}