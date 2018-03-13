using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Agent.Worker.Release;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseCommandExtension : AgentService, IWorkerCommandExtension
    {
        public Type ExtensionType => typeof(IWorkerCommandExtension);

        public string CommandArea => "release";

        public HostTypes SupportedHostTypes => HostTypes.Release | HostTypes.Deployment;

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            if (string.Equals(command.Event, WellKnownReleaseCommand.UpdateReleaseName, StringComparison.OrdinalIgnoreCase))
            {
                ProcessReleaseUpdateReleaseNameCommand(context, command.Data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("ReleaseCommandNotFound", command.Event));
            }
        }

        private void ProcessReleaseUpdateReleaseNameCommand(IExecutionContext context, string data)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(context.Endpoints, nameof(context.Endpoints));

            Guid projectId = context.Variables.System_TeamProjectId ?? Guid.Empty;
            ArgUtil.NotEmpty(projectId, nameof(projectId));
            
            string releaseId = context.Variables.Release_ReleaseId;
            ArgUtil.NotNull(releaseId, nameof(releaseId));

            if (!String.IsNullOrEmpty(data))
            {
                // queue async command task to update release name.
                context.Debug($"Update release name for release: {releaseId} to: {data} at backend.");
                var commandContext = HostContext.CreateService<IAsyncCommandContext>();
                commandContext.InitializeCommandContext(context, StringUtil.Loc("UpdateReleaseName"));
                commandContext.Task = UpdateReleaseNameAsync(commandContext,
                                                             context,
                                                             WorkerUtilities.GetVssConnection(context),
                                                             projectId,
                                                             releaseId,
                                                             data,
                                                             context.CancellationToken);
                context.AsyncCommands.Add(commandContext);
            }
            else
            {
                throw new Exception(StringUtil.Loc("ReleaseNameRequired"));
            }
        }

        private async Task UpdateReleaseNameAsync(
            IAsyncCommandContext commandContext,
            IExecutionContext context,
            VssConnection connection,
            Guid projectId,
            string releaseId,
            string releaseName,
            CancellationToken cancellationToken)
        {
            ReleaseServer releaseServer = new ReleaseServer(connection, projectId);
            var release = await releaseServer.UpdateReleaseName(releaseId, releaseName, cancellationToken);
            commandContext.Output(StringUtil.Loc("UpdateReleaseNameForRelease", release.Name, release.Id));
            context.Variables.Set("release.releaseName", release.Name);
        }
    }    

    internal static class WellKnownReleaseCommand
    {
        public static readonly string UpdateReleaseName = "updatereleasename";
    }
}