using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(JobRunner))]
    public interface IJobRunner : IAgentService
    {
        Task<TaskResult> RunAsync(AgentJobRequestMessage message, CancellationToken jobRequestCancellationToken);
    }

    public sealed class JobRunner : AgentService, IJobRunner
    {
        private IJobServerQueue _jobServerQueue;

        public async Task<TaskResult> RunAsync(AgentJobRequestMessage message, CancellationToken jobRequestCancellationToken)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            ArgUtil.NotNull(message.Environment.Variables, nameof(message.Environment.Variables));
            ArgUtil.NotNull(message.Tasks, nameof(message.Tasks));
            Trace.Info("Job ID {0}", message.JobId);

            // System.AccessToken
            if (message.Environment.Variables.ContainsKey(Constants.Variables.System.EnableAccessToken) &&
                StringUtil.ConvertToBoolean(message.Environment.Variables[Constants.Variables.System.EnableAccessToken]))
            {
                // TODO: get access token use Util Method
                message.Environment.Variables[Constants.Variables.System.AccessToken] = message.Environment.SystemConnection.Authorization.Parameters["AccessToken"];
            }

            // Make sure SystemConnection Url and Endpoint Url match Config Url base
            ReplaceConfigUriBaseInJobRequestMessage(message);

            // Setup the job server and job server queue.
            var jobServer = HostContext.GetService<IJobServer>();
            VssCredentials jobServerCredential = ApiUtil.GetVssCredential(message.Environment.SystemConnection);
            Uri jobServerUrl = message.Environment.SystemConnection.Url;

            Trace.Info($"Creating job server with URL: {jobServerUrl}");
            // jobServerQueue is the throttling reporter.
            _jobServerQueue = HostContext.GetService<IJobServerQueue>();
            VssConnection jobConnection = ApiUtil.CreateConnection(jobServerUrl, jobServerCredential, new DelegatingHandler[] { new ThrottlingReportHandler(_jobServerQueue) });
            await jobServer.ConnectAsync(jobConnection);

            _jobServerQueue.Start(message);

            IExecutionContext jobContext = null;
            try
            {
                // Create the job execution context.
                jobContext = HostContext.CreateService<IExecutionContext>();
                jobContext.InitializeJob(message, jobRequestCancellationToken);
                Trace.Info("Starting the job execution context.");
                jobContext.Start();
                jobContext.Section(StringUtil.Loc("StepStarting", message.JobName));

                // Set agent version variable.
                jobContext.Variables.Set(Constants.Variables.Agent.Version, Constants.Agent.Version);
                jobContext.Output(StringUtil.Loc("AgentVersion", Constants.Agent.Version));

                // Print proxy setting information for better diagnostic experience
                var proxyConfig = HostContext.GetService<IProxyConfiguration>();
                if (!string.IsNullOrEmpty(proxyConfig.ProxyUrl))
                {
                    jobContext.Output(StringUtil.Loc("AgentRunningBehindProxy", proxyConfig.ProxyUrl));
                }

                // Validate directory permissions.
                string workDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                Trace.Info($"Validating directory permissions for: '{workDirectory}'");
                try
                {
                    Directory.CreateDirectory(workDirectory);
                    IOUtil.ValidateExecutePermission(workDirectory);
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                    jobContext.Error(ex);
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Failed);
                }

                // Set agent variables.
                AgentSettings settings = HostContext.GetService<IConfigurationStore>().GetSettings();
                jobContext.Variables.Set(Constants.Variables.Agent.Id, settings.AgentId.ToString(CultureInfo.InvariantCulture));
                jobContext.Variables.Set(Constants.Variables.Agent.HomeDirectory, IOUtil.GetRootPath());
                jobContext.Variables.Set(Constants.Variables.Agent.JobName, message.JobName);
                jobContext.Variables.Set(Constants.Variables.Agent.MachineName, Environment.MachineName);
                jobContext.Variables.Set(Constants.Variables.Agent.Name, settings.AgentName);
                jobContext.Variables.Set(Constants.Variables.Agent.RootDirectory, IOUtil.GetWorkPath(HostContext));
#if OS_WINDOWS
                jobContext.Variables.Set(Constants.Variables.Agent.ServerOMDirectory, Path.Combine(IOUtil.GetExternalsPath(), Constants.Path.ServerOMDirectory));
#endif
                jobContext.Variables.Set(Constants.Variables.Agent.WorkFolder, IOUtil.GetWorkPath(HostContext));
                jobContext.Variables.Set(Constants.Variables.System.WorkFolder, IOUtil.GetWorkPath(HostContext));

                // prefer task definitions url, then TFS collection url, then TFS account url
                var taskServer = HostContext.GetService<ITaskServer>();
                Uri taskServerUri = null;
                if (!string.IsNullOrEmpty(jobContext.Variables.System_TaskDefinitionsUri))
                {
                    taskServerUri = new Uri(jobContext.Variables.System_TaskDefinitionsUri);
                }
                else if (!string.IsNullOrEmpty(jobContext.Variables.System_TFCollectionUrl))
                {
                    taskServerUri = new Uri(jobContext.Variables.System_TFCollectionUrl);
                }

                var taskServerCredential = ApiUtil.GetVssCredential(message.Environment.SystemConnection);
                if (taskServerUri != null)
                {
                    Trace.Info($"Creating task server with {taskServerUri}");
                    await taskServer.ConnectAsync(ApiUtil.CreateConnection(taskServerUri, taskServerCredential));
                }

                if (taskServerUri == null || !await taskServer.TaskDefinitionEndpointExist(jobRequestCancellationToken))
                {
                    Trace.Info($"Can't determine task download url from JobMessage or the endpoint doesn't exist.");
                    var configStore = HostContext.GetService<IConfigurationStore>();
                    taskServerUri = new Uri(configStore.GetSettings().ServerUrl);
                    Trace.Info($"Recreate task server with configuration server url: {taskServerUri}");
                    await taskServer.ConnectAsync(ApiUtil.CreateConnection(taskServerUri, taskServerCredential));
                }

                // Expand the endpoint data values.
                foreach (ServiceEndpoint endpoint in jobContext.Endpoints)
                {
                    jobContext.Variables.ExpandValues(target: endpoint.Data);
                    VarUtil.ExpandEnvironmentVariables(HostContext, target: endpoint.Data);
                }

                // Get the job extension.
                Trace.Info("Getting job extension.");
                string hostType = jobContext.Variables.System_HostType;
                var extensionManager = HostContext.GetService<IExtensionManager>();
                // We should always have one job extension
                IJobExtension jobExtension =
                    (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                    .Where(x => string.Equals(x.HostType, hostType, StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();
                ArgUtil.NotNull(jobExtension, nameof(jobExtension));

                List<IStep> steps = new List<IStep>();

#if OS_WINDOWS
                // Init script job extention.
                // This is for internal testing and is not publicly supported. This will be removed from the agent at a later time.
                var prepareScript = Environment.GetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK");
                if (!string.IsNullOrEmpty(prepareScript))
                {
                    var prepareStep = new ManagementScriptStep(
                        scriptPath: prepareScript,
                        continueOnError: false,
                        critical: true,
                        displayName: "Agent Initialization",
                        enabled: true,
                        @finally: false);

                    Trace.Verbose($"Adding agent init script step.");
                    prepareStep.Initialize(HostContext);
                    prepareStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), prepareStep.DisplayName);
                    prepareStep.AccessToken = message.Environment.SystemConnection.Authorization.Parameters["AccessToken"];
                    steps.Add(prepareStep);
                }
#endif

                // Download tasks if not already in the cache
                Trace.Info("Downloading task definitions.");
                var taskManager = HostContext.GetService<ITaskManager>();
                try
                {
                    await taskManager.DownloadAsync(jobContext, message.Tasks);
                }
                catch (OperationCanceledException ex) when (jobContext.CancellationToken.IsCancellationRequested)
                {
                    // set the job to canceled
                    // don't log error issue to job ExecutionContext, since server owns the job level issue
                    Trace.Error($"Caught exception: {ex}");
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Canceled);
                }
                catch (Exception ex)
                {
                    // Log the error and fail the job.
                    Trace.Error($"Caught exception from {nameof(TaskManager)}: {ex}");
                    jobContext.Error(ex);
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Failed);
                }

                // Add pre-job step from Job Extension
                Trace.Info("Adding pre-job step from extension.");
                if (jobExtension.PreJobStep != null)
                {
                    Trace.Verbose($"Adding {jobExtension.GetType().Name}.{nameof(jobExtension.PreJobStep)}.");
                    jobExtension.PreJobStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), jobExtension.PreJobStep.DisplayName);
                    steps.Add(jobExtension.PreJobStep);
                }

                // Add pre-job steps from Tasks
                Trace.Info("Adding pre-job steps from tasks.");
                steps.AddRange(taskManager.GetTasksPreJobSteps(jobContext, message.Tasks));

                // Add execution step from Job Extension
                Trace.Info("Adding execution step from extension.");
                if (jobExtension.ExecutionStep != null)
                {
                    Trace.Verbose($"Adding {jobExtension.GetType().Name}.{nameof(jobExtension.ExecutionStep)}.");
                    jobExtension.ExecutionStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), jobExtension.ExecutionStep.DisplayName);
                    steps.Add(jobExtension.ExecutionStep);
                }

                // Add execution steps from Tasks
                Trace.Info("Adding tasks.");
                steps.AddRange(taskManager.GetTasksMainSteps(jobContext, message.Tasks));

                // Add post-job steps from Tasks
                Trace.Info("Adding post-job steps from tasks.");
                steps.AddRange(taskManager.GetTasksPostJobSteps(jobContext, message.Tasks));

                // Add post-job steps from Job Extension
                Trace.Info("Adding  post-job step from extension.");
                if (jobExtension.PostJobStep != null)
                {
                    Trace.Verbose($"Adding {jobExtension.GetType().Name}.{nameof(jobExtension.PostJobStep)}.");
                    jobExtension.PostJobStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), jobExtension.PostJobStep.DisplayName);
                    steps.Add(jobExtension.PostJobStep);
                }

#if OS_WINDOWS
                // Add script post steps.
                // This is for internal testing and is not publicly supported. This will be removed from the agent at a later time.
                var finallyScript = Environment.GetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK");
                if (!string.IsNullOrEmpty(finallyScript))
                {
                    var finallyStep = new ManagementScriptStep(
                        scriptPath: finallyScript,
                        continueOnError: false,
                        critical: true,
                        displayName: "Agent Cleanup",
                        enabled: true,
                        @finally: true);

                    Trace.Verbose($"Adding agent cleanup script step.");
                    finallyStep.Initialize(HostContext);
                    finallyStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), finallyStep.DisplayName);
                    finallyStep.AccessToken = message.Environment.SystemConnection.Authorization.Parameters["AccessToken"];
                    steps.Add(finallyStep);
                }
#endif

                // Run the steps.
                var stepsRunner = HostContext.GetService<IStepsRunner>();
                try
                {
                    await stepsRunner.RunAsync(jobContext, steps);
                }
                catch (OperationCanceledException ex) when (jobContext.CancellationToken.IsCancellationRequested)
                {
                    // set the job to canceled
                    // don't log error issue to job ExecutionContext, since server owns the job level issue
                    Trace.Error($"Caught exception: {ex}");
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Canceled);
                }
                catch (Exception ex)
                {
                    // Log the error and fail the job.
                    Trace.Error($"Caught exception from {nameof(StepsRunner)}: {ex}");
                    jobContext.Error(ex);
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Failed);
                }

                Trace.Info($"Job result: {jobContext.Result}");

                // Complete the job.
                Trace.Info("Completing the job execution context.");
                return await CompleteJobAsync(jobServer, jobContext, message);
            }
            finally
            {
                await ShutdownQueue();
            }
        }

        private async Task<TaskResult> CompleteJobAsync(IJobServer jobServer, IExecutionContext jobContext, AgentJobRequestMessage message, TaskResult? taskResult = null)
        {
            jobContext.Section(StringUtil.Loc("StepFinishing", message.JobName));
            TaskResult result = jobContext.Complete(taskResult);

            if (!jobContext.Features.HasFlag(PlanFeatures.JobCompletedPlanEvent))
            {
                Trace.Info($"Skip raise job completed event call from worker because Plan version is {message.Plan.Version}");
                return result;
            }

            await ShutdownQueue();

            Trace.Info("Raising job completed event.");
            IEnumerable<Variable> outputVariables = jobContext.Variables.GetOutputVariables();
            //var webApiVariables = outputVariables.ToJobCompletedEventOutputVariables();
            //var jobCompletedEvent = new JobCompletedEvent(message.RequestId, message.JobId, result, webApiVariables);
            var jobCompletedEvent = new JobCompletedEvent(message.RequestId, message.JobId, result);

            var completeJobRetryLimit = 5;
            var exceptions = new List<Exception>();
            while (completeJobRetryLimit-- > 0)
            {
                try
                {
                    await jobServer.RaisePlanEventAsync(message.Plan.ScopeIdentifier, message.Plan.PlanType, message.Plan.PlanId, jobCompletedEvent, default(CancellationToken));
                    return result;
                }
                catch (TaskOrchestrationPlanNotFoundException ex)
                {
                    Trace.Error($"TaskOrchestrationPlanNotFoundException received, while attempting to raise JobCompletedEvent for job {message.JobId}. Error: {ex}");
                    return TaskResult.Failed;
                }
                catch (TaskOrchestrationPlanSecurityException ex)
                {
                    Trace.Error($"TaskOrchestrationPlanSecurityException received, while attempting to raise JobCompletedEvent for job {message.JobId}. Error: {ex}");
                    return TaskResult.Failed;
                }
                catch (Exception ex)
                {
                    Trace.Error($"Catch exception while attempting to raise JobCompletedEvent for job {message.JobId}, job request {message.RequestId}.");
                    Trace.Error(ex);
                    exceptions.Add(ex);
                }

                // delay 5 seconds before next retry.
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            // rethrow exceptions from all attempts.
            throw new AggregateException(exceptions);
        }

        private async Task ShutdownQueue()
        {
            if (_jobServerQueue != null)
            {
                try
                {
                    Trace.Info("Shutting down the job server queue.");
                    await _jobServerQueue.ShutdownAsync();
                }
                catch (Exception ex)
                {
                    Trace.Error($"Caught exception from {nameof(JobServerQueue)}.{nameof(_jobServerQueue.ShutdownAsync)}: {ex}");
                }
                finally
                {
                    _jobServerQueue = null; // Prevent multiple attempts.
                }
            }
        }

        // the hostname (how the agent knows the server) is external to our server
        // in other words, an agent may have it's own way (DNS, hostname) of refering
        // to the server.  it owns that.  That's the hostname we will use.
        // Example: Server's notification url is http://tfsserver:8080/tfs 
        //          Agent config url is http://tfsserver.mycompany.com:8080/tfs 
        private Uri ReplaceWithConfigUriBase(Uri messageUri)
        {
            AgentSettings settings = HostContext.GetService<IConfigurationStore>().GetSettings();
            try
            {
                if (UrlUtil.IsHosted(messageUri.AbsoluteUri))
                {
                    // If messageUri is hosted service URL, return the messageUri as it is.
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

        private void ReplaceConfigUriBaseInJobRequestMessage(AgentJobRequestMessage message)
        {
            string systemConnectionHostName = message.Environment.SystemConnection.Url.GetComponents(UriComponents.Host, UriFormat.Unescaped);
            // fixup any endpoint Url that match SystemConnect host.
            foreach (var endpoint in message.Environment.Endpoints)
            {
                if (endpoint.Url.GetComponents(UriComponents.Host, UriFormat.Unescaped).Equals(systemConnectionHostName, StringComparison.OrdinalIgnoreCase))
                {
                    endpoint.Url = ReplaceWithConfigUriBase(endpoint.Url);
                    Trace.Info($"Ensure endpoint url match config url base. {endpoint.Url}");
                }
            }

            // fixup well known variables. (taskDefinitionsUrl, tfsServerUrl, tfsCollectionUrl)
            if (message.Environment.Variables.ContainsKey(WellKnownDistributedTaskVariables.TaskDefinitionsUrl))
            {
                string taskDefinitionsUrl = message.Environment.Variables[WellKnownDistributedTaskVariables.TaskDefinitionsUrl];
                message.Environment.Variables[WellKnownDistributedTaskVariables.TaskDefinitionsUrl] = ReplaceWithConfigUriBase(new Uri(taskDefinitionsUrl)).AbsoluteUri;
                Trace.Info($"Ensure System.TaskDefinitionsUrl match config url base. {message.Environment.Variables[WellKnownDistributedTaskVariables.TaskDefinitionsUrl]}");
            }

            if (message.Environment.Variables.ContainsKey(WellKnownDistributedTaskVariables.TFCollectionUrl))
            {
                string tfsCollectionUrl = message.Environment.Variables[WellKnownDistributedTaskVariables.TFCollectionUrl];
                message.Environment.Variables[WellKnownDistributedTaskVariables.TFCollectionUrl] = ReplaceWithConfigUriBase(new Uri(tfsCollectionUrl)).AbsoluteUri;
                Trace.Info($"Ensure System.TFCollectionUrl match config url base. {message.Environment.Variables[WellKnownDistributedTaskVariables.TFCollectionUrl]}");
            }

            // fixup SystemConnection Url
            message.Environment.SystemConnection.Url = ReplaceWithConfigUriBase(message.Environment.SystemConnection.Url);
            Trace.Info($"Ensure SystemConnection url match config url base. {message.Environment.SystemConnection.Url}");

            // back compat server url
            message.Environment.Variables[Constants.Variables.System.TFServerUrl] = message.Environment.SystemConnection.Url.AbsoluteUri;
            Trace.Info($"Ensure System.TFServerUrl match config url base. {message.Environment.SystemConnection.Url.AbsoluteUri}");
        }
    }
}
