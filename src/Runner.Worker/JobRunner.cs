using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common.Util;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.IO.Compression;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.ObjectTemplating;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(JobRunner))]
    public interface IJobRunner : IRunnerService
    {
        Task<TaskResult> RunAsync(Pipelines.AgentJobRequestMessage message, CancellationToken jobRequestCancellationToken);
    }

    public sealed class JobRunner : RunnerService, IJobRunner
    {
        private IJobServerQueue _jobServerQueue;
        private ITempDirectoryManager _tempDirectoryManager;

        public async Task<TaskResult> RunAsync(Pipelines.AgentJobRequestMessage message, CancellationToken jobRequestCancellationToken)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Resources, nameof(message.Resources));
            ArgUtil.NotNull(message.Variables, nameof(message.Variables));
            ArgUtil.NotNull(message.Steps, nameof(message.Steps));
            Trace.Info("Job ID {0}", message.JobId);

            DateTime jobStartTimeUtc = DateTime.UtcNow;

            // Agent.RunMode
            RunMode runMode;
            if (message.Variables.ContainsKey(Constants.Variables.Agent.RunMode) &&
                Enum.TryParse(message.Variables[Constants.Variables.Agent.RunMode].Value, ignoreCase: true, result: out runMode) &&
                runMode == RunMode.Local)
            {
                HostContext.RunMode = runMode;
            }

            ServiceEndpoint systemConnection = message.Resources.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));

            // Make sure SystemConnection Url and Endpoint Url match Config Url base for OnPremises server
            // System.ServerType will always be there after M133
            if (!message.Variables.ContainsKey(Constants.Variables.System.ServerType) ||
                string.Equals(message.Variables[Constants.Variables.System.ServerType]?.Value, "OnPremises", StringComparison.OrdinalIgnoreCase))
            {
                ReplaceConfigUriBaseInJobRequestMessage(message);
            }

            // Setup the job server and job server queue.
            var jobServer = HostContext.GetService<IJobServer>();
            VssCredentials jobServerCredential = VssUtil.GetVssCredential(systemConnection);
            Uri jobServerUrl = systemConnection.Url;

            Trace.Info($"Creating job server with URL: {jobServerUrl}");
            // jobServerQueue is the throttling reporter.
            _jobServerQueue = HostContext.GetService<IJobServerQueue>();
            VssConnection jobConnection = VssUtil.CreateConnection(jobServerUrl, jobServerCredential, new DelegatingHandler[] { new ThrottlingReportHandler(_jobServerQueue) });
            await jobServer.ConnectAsync(jobConnection);

            MakeJobMessageCompat(message);

            _jobServerQueue.Start(message);
            HostContext.WritePerfCounter($"WorkerJobServerQueueStarted_{message.RequestId.ToString()}");

            IExecutionContext jobContext = null;
            CancellationTokenRegistration? runnerShutdownRegistration = null;
            try
            {
                // Create the job execution context.
                jobContext = HostContext.CreateService<IExecutionContext>();
                jobContext.InitializeJob(message, jobRequestCancellationToken);
                Trace.Info("Starting the job execution context.");
                jobContext.Start();
                jobContext.Section(StringUtil.Loc("StepStarting", message.JobDisplayName));

                runnerShutdownRegistration = HostContext.RunnerShutdownToken.Register(() =>
                {
                    // log an issue, then runner get shutdown by Ctrl-C or Ctrl-Break.
                    // the server will use Ctrl-Break to tells the runner that operating system is shutting down.
                    string errorMessage;
                    switch (HostContext.RunnerShutdownReason)
                    {
                        case ShutdownReason.UserCancelled:
                            errorMessage = StringUtil.Loc("UserShutdownRunner");
                            break;
                        case ShutdownReason.OperatingSystemShutdown:
                            errorMessage = StringUtil.Loc("OperatingSystemShutdown", Environment.MachineName);
                            break;
                        default:
                            throw new ArgumentException(HostContext.RunnerShutdownReason.ToString(), nameof(HostContext.RunnerShutdownReason));
                    }
                    jobContext.AddIssue(new Issue() { Type = IssueType.Error, Message = errorMessage });
                });

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

                string toolsDirectory = HostContext.GetDirectory(WellKnownDirectory.Tools);
                Directory.CreateDirectory(toolsDirectory);
                jobContext.SetRunnerContext("toolsdirectory", toolsDirectory);
                jobContext.SetRunnerContext("workfolder", HostContext.GetDirectory(WellKnownDirectory.Work));
                jobContext.SetRunnerContext("version", BuildConstants.RunnerPackage.Version);

                // Setup TEMP directories
                _tempDirectoryManager = HostContext.GetService<ITempDirectoryManager>();
                _tempDirectoryManager.InitializeTempDirectory(jobContext);

                // // Expand container properties
                // jobContext.Container?.ExpandProperties(jobContext.Variables);
                // foreach (var sidecar in jobContext.SidecarContainers)
                // {
                //     sidecar.ExpandProperties(jobContext.Variables);
                // }

                // Get the job extension.
                Trace.Info("Getting job extension.");
                IJobExtension jobExtension = HostContext.CreateService<IJobExtension>();
                List<IStep> jobSteps = null;
                try
                {
                    Trace.Info("Initialize job. Getting all job steps.");
                    jobSteps = await jobExtension.InitializeJob(jobContext, message);
                }
                catch (OperationCanceledException ex) when (jobContext.CancellationToken.IsCancellationRequested)
                {
                    // set the job to canceled
                    // don't log error issue to job ExecutionContext, since server owns the job level issue
                    Trace.Error($"Job is canceled during initialize.");
                    Trace.Error($"Caught exception: {ex}");
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Canceled);
                }
                catch (Exception ex)
                {
                    // set the job to failed.
                    // don't log error issue to job ExecutionContext, since server owns the job level issue
                    Trace.Error($"Job initialize failed.");
                    Trace.Error($"Caught exception from {nameof(jobExtension.InitializeJob)}: {ex}");
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Failed);
                }

                // trace out all steps
                Trace.Info($"Total job steps: {jobSteps.Count}.");
                Trace.Verbose($"Job steps: '{string.Join(", ", jobSteps.Select(x => x.DisplayName))}'");
                HostContext.WritePerfCounter($"WorkerJobInitialized_{message.RequestId.ToString()}");

                // Run all job steps
                Trace.Info("Run all job steps.");
                var stepsRunner = HostContext.GetService<IStepsRunner>();
                try
                {
                    await stepsRunner.RunAsync(jobContext, jobSteps);
                }
                catch (Exception ex)
                {
                    // StepRunner should never throw exception out.
                    // End up here mean there is a bug in StepRunner
                    // Log the error and fail the job.
                    Trace.Error($"Caught exception from job steps {nameof(StepsRunner)}: {ex}");
                    jobContext.Error(ex);
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Failed);
                }
                finally
                {
                    Trace.Info("Finalize job.");
                    await jobExtension.FinalizeJob(jobContext);
                }

                Trace.Info($"Job result after all job steps finish: {jobContext.Result ?? TaskResult.Succeeded}");

                // if (jobContext.Variables.GetBoolean(Constants.Variables.Agent.Diagnostic) ?? false)
                // {
                //     Trace.Info("Support log upload starting.");

                //     IDiagnosticLogManager diagnosticLogManager = HostContext.GetService<IDiagnosticLogManager>();

                //     try
                //     {
                //         await diagnosticLogManager.UploadDiagnosticLogsAsync(executionContext: jobContext, message: message, jobStartTimeUtc: jobStartTimeUtc);

                //         Trace.Info("Support log upload complete.");
                //     }
                //     catch (Exception ex)
                //     {
                //         // Log the error but make sure we continue gracefully.
                //         Trace.Info("Error uploading support logs.");
                //         Trace.Error(ex);
                //     }
                // }

                Trace.Info("Completing the job execution context.");
                return await CompleteJobAsync(jobServer, jobContext, message);
            }
            finally
            {
                if (runnerShutdownRegistration != null)
                {
                    runnerShutdownRegistration.Value.Dispose();
                    runnerShutdownRegistration = null;
                }

                await ShutdownQueue(throwOnFailure: false);
            }
        }

        private async Task<TaskResult> CompleteJobAsync(IJobServer jobServer, IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, TaskResult? taskResult = null)
        {
            jobContext.Section(StringUtil.Loc("StepFinishing", message.JobDisplayName));
            TaskResult result = jobContext.Complete(taskResult);

            try
            {
                await ShutdownQueue(throwOnFailure: true);
            }
            catch (Exception ex)
            {
                Trace.Error($"Caught exception from {nameof(JobServerQueue)}.{nameof(_jobServerQueue.ShutdownAsync)}");
                Trace.Error("This indicate a failure during publish output variables. Fail the job to prevent unexpected job outputs.");
                Trace.Error(ex);
                result = TaskResultUtil.MergeTaskResults(result, TaskResult.Failed);
            }

            // Clean TEMP after finish process jobserverqueue, since there might be a pending fileupload still use the TEMP dir.
            _tempDirectoryManager?.CleanupTempDirectory();

            if (!jobContext.Features.HasFlag(PlanFeatures.JobCompletedPlanEvent))
            {
                Trace.Info($"Skip raise job completed event call from worker because Plan version is {message.Plan.Version}");
                return result;
            }

            Trace.Info("Raising job completed event.");
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
                    Trace.Error($"TaskOrchestrationPlanNotFoundException received, while attempting to raise JobCompletedEvent for job {message.JobId}.");
                    Trace.Error(ex);
                    return TaskResult.Failed;
                }
                catch (TaskOrchestrationPlanSecurityException ex)
                {
                    Trace.Error($"TaskOrchestrationPlanSecurityException received, while attempting to raise JobCompletedEvent for job {message.JobId}.");
                    Trace.Error(ex);
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

        private async Task ShutdownQueue(bool throwOnFailure)
        {
            if (_jobServerQueue != null)
            {
                try
                {
                    Trace.Info("Shutting down the job server queue.");
                    await _jobServerQueue.ShutdownAsync();
                }
                catch (Exception ex) when (!throwOnFailure)
                {
                    Trace.Error($"Caught exception from {nameof(JobServerQueue)}.{nameof(_jobServerQueue.ShutdownAsync)}");
                    Trace.Error(ex);
                }
                finally
                {
                    _jobServerQueue = null; // Prevent multiple attempts.
                }
            }
        }

        // the scheme://hostname:port (how the agent knows the server) is external to our server
        // in other words, an agent may have it's own way (DNS, hostname) of refering
        // to the server.  it owns that.  That's the scheme://hostname:port we will use.
        // Example: Server's notification url is http://tfsserver:8080/tfs 
        //          Agent config url is https://tfsserver.mycompany.com:9090/tfs 
        private Uri ReplaceWithConfigUriBase(Uri messageUri)
        {
            RunnerSettings settings = HostContext.GetService<IConfigurationStore>().GetSettings();
            try
            {
                Uri result = null;
                Uri configUri = new Uri(settings.ServerUrl);
                if (Uri.TryCreate(new Uri(configUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped)), messageUri.PathAndQuery, out result))
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

        private void ReplaceConfigUriBaseInJobRequestMessage(Pipelines.AgentJobRequestMessage message)
        {
            ServiceEndpoint systemConnection = message.Resources.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            Uri systemConnectionUrl = systemConnection.Url;

            // fixup any endpoint Url that match SystemConnection Url.
            foreach (var endpoint in message.Resources.Endpoints)
            {
                if (Uri.Compare(endpoint.Url, systemConnectionUrl, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    endpoint.Url = ReplaceWithConfigUriBase(endpoint.Url);
                    Trace.Info($"Ensure endpoint url match config url base. {endpoint.Url}");
                }
            }

            // fixup any repository Url that match SystemConnection Url.
            foreach (var repo in message.Resources.Repositories)
            {
                if (Uri.Compare(repo.Url, systemConnectionUrl, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    repo.Url = ReplaceWithConfigUriBase(repo.Url);
                    Trace.Info($"Ensure repository url match config url base. {repo.Url}");
                }
            }

            // fixup well known variables. (taskDefinitionsUrl, tfsServerUrl, tfsCollectionUrl)
            if (message.Variables.ContainsKey(WellKnownDistributedTaskVariables.TaskDefinitionsUrl))
            {
                string taskDefinitionsUrl = message.Variables[WellKnownDistributedTaskVariables.TaskDefinitionsUrl].Value;
                message.Variables[WellKnownDistributedTaskVariables.TaskDefinitionsUrl] = ReplaceWithConfigUriBase(new Uri(taskDefinitionsUrl)).AbsoluteUri;
                Trace.Info($"Ensure System.TaskDefinitionsUrl match config url base. {message.Variables[WellKnownDistributedTaskVariables.TaskDefinitionsUrl].Value}");
            }

            if (message.Variables.ContainsKey(WellKnownDistributedTaskVariables.TFCollectionUrl))
            {
                string tfsCollectionUrl = message.Variables[WellKnownDistributedTaskVariables.TFCollectionUrl].Value;
                message.Variables[WellKnownDistributedTaskVariables.TFCollectionUrl] = ReplaceWithConfigUriBase(new Uri(tfsCollectionUrl)).AbsoluteUri;
                Trace.Info($"Ensure System.TFCollectionUrl match config url base. {message.Variables[WellKnownDistributedTaskVariables.TFCollectionUrl].Value}");
            }

            if (message.Variables.ContainsKey(Constants.Variables.System.TFServerUrl))
            {
                string tfsServerUrl = message.Variables[Constants.Variables.System.TFServerUrl].Value;
                message.Variables[Constants.Variables.System.TFServerUrl] = ReplaceWithConfigUriBase(new Uri(tfsServerUrl)).AbsoluteUri;
                Trace.Info($"Ensure System.TFServerUrl match config url base. {message.Variables[Constants.Variables.System.TFServerUrl].Value}");
            }
        }

        private void MakeJobMessageCompat(Pipelines.AgentJobRequestMessage message)
        {
            var steps = new List<Pipelines.JobStep>();
            foreach (var action in message.Steps.OfType<Pipelines.ActionStep>())
            {
                if (action.Reference.Type == Pipelines.ActionSourceType.AgentPlugin)
                {
                    bool fixInput = true;
                    var inputs = TemplateUtil.AssertMapping(action.Inputs, "inputs");
                    foreach (var input in inputs)
                    {
                        var inputName = TemplateUtil.AssertLiteral(input.Key, "input");
                        if (string.Equals(inputName.Value, Pipelines.PipelineConstants.CheckoutTaskInputs.Version, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(inputName.Value, Pipelines.PipelineConstants.CheckoutTaskInputs.Ref, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(inputName.Value, Pipelines.PipelineConstants.CheckoutTaskInputs.Token, StringComparison.OrdinalIgnoreCase))
                        {
                            fixInput = false;
                            break;
                        }
                    }

                    if (fixInput)
                    {
                        var newInputs = new MappingToken(null, null, null);
                        newInputs.Add(new LiteralToken(null, null, null, Pipelines.PipelineConstants.CheckoutTaskInputs.Repository), new BasicExpressionToken(null, null, null, "github.repository"));
                        newInputs.Add(new LiteralToken(null, null, null, Pipelines.PipelineConstants.CheckoutTaskInputs.Ref), new BasicExpressionToken(null, null, null, "github.ref"));
                        newInputs.Add(new LiteralToken(null, null, null, Pipelines.PipelineConstants.CheckoutTaskInputs.Version), new BasicExpressionToken(null, null, null, "github.sha"));
                        newInputs.Add(new LiteralToken(null, null, null, Pipelines.PipelineConstants.CheckoutTaskInputs.Token), new BasicExpressionToken(null, null, null, "github.token"));
                        newInputs.Add(new LiteralToken(null, null, null, Pipelines.PipelineConstants.CheckoutTaskInputs.WorkspaceRepo), new LiteralToken(null, null, null, bool.TrueString));
                        action.Inputs = newInputs;
                    }
                }

                steps.Add(action);
            }

            message.Steps.Clear();
            message.Steps.AddRange(steps);
        }
    }
}
