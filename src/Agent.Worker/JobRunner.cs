using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(JobRunner))]
    public interface IJobRunner : IAgentService
    {
        Task<TaskResult> RunAsync(JobRequestMessage message, CancellationToken jobRequestCancellationToken);
    }

    public sealed class JobRunner : AgentService, IJobRunner
    {
        private const string _releaseManagementUrlSuffix = "vsrm.visualstudio.com";

        public async Task<TaskResult> RunAsync(JobRequestMessage message, CancellationToken jobRequestCancellationToken)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            ArgUtil.NotNull(message.Environment.Variables, nameof(message.Environment.Variables));
            ArgUtil.NotNull(message.Tasks, nameof(message.Tasks));
            Trace.Info("Job ID {0}", message.JobId);

            if (message.Environment.Variables.ContainsKey(Constants.Variables.System.EnableAccessToken) &&
                StringUtil.ConvertToBoolean(message.Environment.Variables[Constants.Variables.System.EnableAccessToken]))
            {
                // TODO: get access token use Util Method
                message.Environment.Variables[Constants.Variables.System.AccessToken] = message.Environment.SystemConnection.Authorization.Parameters["AccessToken"];
            }

            // Setup the job server and job server queue.
            var jobServer = HostContext.GetService<IJobServer>();
            var jobServerCredential = ApiUtil.GetVssCredential(message.Environment.SystemConnection);
            Uri jobServerUrl = ReplaceWithConfigUriBase(message.Environment.SystemConnection.Url);

            Trace.Info($"Creating job server with URL: {jobServerUrl}");
            var jobConnection = ApiUtil.CreateConnection(jobServerUrl, jobServerCredential);
            await jobServer.ConnectAsync(jobConnection);

            var jobServerQueue = HostContext.GetService<IJobServerQueue>();
            jobServerQueue.Start(message);

            IExecutionContext jobContext = null;
            try
            {
                // Create the job execution context.
                jobContext = HostContext.CreateService<IExecutionContext>();
                jobContext.InitializeJob(message, jobRequestCancellationToken);
                Trace.Info("Starting the job execution context.");
                jobContext.Start();

                // Set agent variables.
                AgentSettings settings = HostContext.GetService<IConfigurationStore>().GetSettings();
                jobContext.Variables.Set(Constants.Variables.Agent.Id, settings.AgentId.ToString(CultureInfo.InvariantCulture));
                jobContext.Variables.Set(Constants.Variables.Agent.HomeDirectory, IOUtil.GetRootPath());
                jobContext.Variables.Set(Constants.Variables.Agent.JobName, message.JobName);
                jobContext.Variables.Set(Constants.Variables.Agent.MachineName, Environment.MachineName);
                jobContext.Variables.Set(Constants.Variables.Agent.Name, settings.AgentName);
                jobContext.Variables.Set(Constants.Variables.Agent.RootDirectory, IOUtil.GetWorkPath(HostContext));
                jobContext.Variables.Set(Constants.Variables.Agent.ServerOMDirectory, Path.Combine(IOUtil.GetExternalsPath(), "vstsom"));
                jobContext.Variables.Set(Constants.Variables.Agent.WorkFolder, IOUtil.GetWorkPath(HostContext));
                jobContext.Variables.Set(Constants.Variables.System.WorkFolder, IOUtil.GetWorkPath(HostContext));

                // prefer task definitions url, then TFS collection url, then TFS account url
                var taskServer = HostContext.GetService<ITaskServer>();
                Uri taskServerUri;
                if (!string.IsNullOrEmpty(jobContext.Variables.System_TaskDefinitionsUri))
                {
                    taskServerUri = ReplaceWithConfigUriBase(new Uri(jobContext.Variables.System_TaskDefinitionsUri));
                }
                else if (!string.IsNullOrEmpty(jobContext.Variables.System_TFCollectionUrl))
                {
                    taskServerUri = ReplaceWithConfigUriBase(new Uri(jobContext.Variables.System_TFCollectionUrl));
                }
                else
                {
                    var configStore = HostContext.GetService<IConfigurationStore>();
                    taskServerUri = ReplaceWithConfigUriBase(new Uri(configStore.GetSettings().ServerUrl));
                }

                Trace.Info($"Creating task server with {taskServerUri}");
                var taskServerCredential = ApiUtil.GetVssCredential(message.Environment.SystemConnection);
                await taskServer.ConnectAsync(ApiUtil.CreateConnection(taskServerUri, taskServerCredential));

                // Expand the endpoint data values.
                foreach (ServiceEndpoint endpoint in jobContext.Endpoints)
                {
                    jobContext.Variables.ExpandValues(target: endpoint.Data);
                    VarUtil.ExpandEnvironmentVariables(HostContext, target: endpoint.Data);
                }

                // Get the job extensions.
                Trace.Info("Getting job extensions.");
                string hostType = jobContext.Variables.System_HostType;
                var extensionManager = HostContext.GetService<IExtensionManager>();
                IJobExtension[] extensions =
                    (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                    .Where(x => string.Equals(x.HostType, hostType, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // Add the prepare steps.
                Trace.Info("Adding job prepare extensions.");
                List<IStep> steps = new List<IStep>();
                foreach (IJobExtension extension in extensions)
                {
                    if (extension.PrepareStep != null)
                    {
                        Trace.Verbose($"Adding {extension.GetType().Name}.{nameof(extension.PrepareStep)}.");
                        extension.PrepareStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), extension.PrepareStep.DisplayName);
                        steps.Add(extension.PrepareStep);
                    }
                }

                // Add the task steps.
                Trace.Info("Adding tasks.");
                foreach (TaskInstance taskInstance in message.Tasks)
                {
                    Trace.Verbose($"Adding {taskInstance.DisplayName}.");
                    var taskRunner = HostContext.CreateService<ITaskRunner>();
                    taskRunner.ExecutionContext = jobContext.CreateChild(taskInstance.InstanceId, taskInstance.DisplayName);
                    taskRunner.TaskInstance = taskInstance;
                    steps.Add(taskRunner);
                }

                // Add the finally steps.
                Trace.Info("Adding job finally extensions.");
                foreach (IJobExtension extension in extensions)
                {
                    if (extension.FinallyStep != null)
                    {
                        Trace.Verbose($"Adding {extension.GetType().Name}.{nameof(extension.FinallyStep)}.");
                        extension.FinallyStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), extension.FinallyStep.DisplayName);
                        steps.Add(extension.FinallyStep);
                    }
                }

                // Download tasks if not already in the cache
                Trace.Info("Downloading task definitions.");
                var taskManager = HostContext.GetService<ITaskManager>();
                try
                {
                    await taskManager.DownloadAsync(jobContext, message.Tasks);
                }
                catch (OperationCanceledException ex)
                {
                    // set the job to canceled
                    Trace.Error($"Caught exception: {ex}");
                    jobContext.Error(ex);
                    return jobContext.Complete(TaskResult.Canceled);
                }
                catch (Exception ex)
                {
                    // Log the error and fail the job.
                    Trace.Error($"Caught exception from {nameof(TaskManager)}: {ex}");
                    jobContext.Error(ex);
                    return jobContext.Complete(TaskResult.Failed);
                }

                // Run the steps.
                var stepsRunner = HostContext.GetService<IStepsRunner>();
                try
                {
                    await stepsRunner.RunAsync(jobContext, steps);
                }
                catch (OperationCanceledException ex)
                {
                    // set the job to canceled
                    Trace.Error($"Caught exception: {ex}");
                    jobContext.Error(ex);
                    return jobContext.Complete(TaskResult.Canceled);
                }
                catch (Exception ex)
                {
                    // Log the error and fail the job.
                    Trace.Error($"Caught exception from {nameof(StepsRunner)}: {ex}");
                    jobContext.Error(ex);
                    return jobContext.Complete(TaskResult.Failed);
                }

                Trace.Info($"Job result: {jobContext.Result}");

                // Complete the job.
                Trace.Info("Completing the job execution context.");
                return jobContext.Complete();
            }
            finally
            {
                // Drain the job server queue.
                if (jobServerQueue != null)
                {
                    try
                    {
                        Trace.Info("Shutting down the job server queue.");
                        await jobServerQueue.ShutdownAsync();
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Caught exception from {nameof(JobServerQueue)}.{nameof(jobServerQueue.ShutdownAsync)}: {ex}");
                    }
                }
            }
        }

        // the hostname (how the agent knows the server) is external to our server
        // in other words, an agent may have it's own way (DNS, hostname) of refering
        // to the server.  it owns that.  That's the hostname we will use
        private Uri ReplaceWithConfigUriBase(Uri messageUri)
        {
            AgentSettings settings = HostContext.GetService<IConfigurationStore>().GetSettings();
            try
            {
                string jobServerHost = messageUri.GetComponents(UriComponents.Host, UriFormat.Unescaped);
                if (!string.IsNullOrEmpty(jobServerHost)
                    && jobServerHost.IndexOf(_releaseManagementUrlSuffix, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    // If its hosted and has RM service URL, return the messageUri as it is.
                    return messageUri;
                }

                var configUri = new Uri(settings.ServerUrl);
                Uri result = null;
                Uri configBaseUri = null;
                string scheme = messageUri.GetComponents(UriComponents.Scheme, UriFormat.Unescaped);
                string host = configUri.GetComponents(UriComponents.Host, UriFormat.Unescaped);

                int portValue = 0;
                string port = messageUri.GetComponents(UriComponents.Port, UriFormat.Unescaped);
                if (!string.IsNullOrEmpty(port))
                {
                    int.TryParse(port, out portValue);
                }

                configBaseUri = portValue > 0 ? new UriBuilder(scheme, host, portValue).Uri : new UriBuilder(scheme, host).Uri;

                if (Uri.TryCreate(configBaseUri, messageUri.PathAndQuery, out result))
                {
                    //replace the schema and host portion of messageUri with the host from the
                    //server URI (which was set at config time)
                    return result;
                }
            }
            catch (InvalidOperationException ex)
            {
                //cannot parse the Uri - not a fatal error
                Trace.Error(ex);
            }
            catch (UriFormatException ex)
            {
                //cannot parse the Uri - not a fatal error
                Trace.Error(ex);
            }

            return messageUri;
        }
    }
}
