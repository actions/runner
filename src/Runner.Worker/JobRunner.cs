using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common.Util;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.Pipelines.ContextData;

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

            ServiceEndpoint systemConnection = message.Resources.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));

            // Setup the job server and job server queue.
            var jobServer = HostContext.GetService<IJobServer>();
            VssCredentials jobServerCredential = VssUtil.GetVssCredential(systemConnection);
            Uri jobServerUrl = systemConnection.Url;

            Trace.Info($"Creating job server with URL: {jobServerUrl}");
            // jobServerQueue is the throttling reporter.
            _jobServerQueue = HostContext.GetService<IJobServerQueue>();
            VssConnection jobConnection = VssUtil.CreateConnection(jobServerUrl, jobServerCredential, new DelegatingHandler[] { new ThrottlingReportHandler(_jobServerQueue) });
            await jobServer.ConnectAsync(jobConnection);

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
                if (!JobPassesSecurityRestrictions(jobContext))
                {
                    var configurationStore = HostContext.GetService<IConfigurationStore>();
                    RunnerSettings settings = configurationStore.GetSettings();
                    jobContext.Error($"Running job on worker {settings.AgentName} disallowed by security policy");
                    return await CompleteJobAsync(jobServer, jobContext, message, TaskResult.Failed);
                }

                jobContext.Debug($"Starting: {message.JobDisplayName}");

                runnerShutdownRegistration = HostContext.RunnerShutdownToken.Register(() =>
                {
                    // log an issue, then runner get shutdown by Ctrl-C or Ctrl-Break.
                    // the server will use Ctrl-Break to tells the runner that operating system is shutting down.
                    string errorMessage;
                    switch (HostContext.RunnerShutdownReason)
                    {
                        case ShutdownReason.UserCancelled:
                            errorMessage = "The runner has received a shutdown signal. This can happen when the runner service is stopped, or a manually started runner is canceled.";
                            break;
                        case ShutdownReason.OperatingSystemShutdown:
                            errorMessage = $"Operating system is shutting down for computer '{Environment.MachineName}'";
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

                if (jobContext.Global.WriteDebug)
                {
                    jobContext.SetRunnerContext("debug", "1");
                }

                jobContext.SetRunnerContext("os", VarUtil.OS);

                string toolsDirectory = HostContext.GetDirectory(WellKnownDirectory.Tools);
                Directory.CreateDirectory(toolsDirectory);
                jobContext.SetRunnerContext("tool_cache", toolsDirectory);

                // Setup TEMP directories
                _tempDirectoryManager = HostContext.GetService<ITempDirectoryManager>();
                _tempDirectoryManager.InitializeTempDirectory(jobContext);

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
                    foreach (var step in jobSteps)
                    {
                        jobContext.JobSteps.Enqueue(step);
                    }

                    await stepsRunner.RunAsync(jobContext);
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
                    jobExtension.FinalizeJob(jobContext, message, jobStartTimeUtc);
                }

                Trace.Info($"Job result after all job steps finish: {jobContext.Result ?? TaskResult.Succeeded}");

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

        private bool JobPassesSecurityRestrictions(IExecutionContext jobContext)
        {
            var gitHubContext = jobContext.ExpressionValues["github"] as GitHubContext;

            try {
              if (gitHubContext.IsPullRequest())
              {
                  return OkayToRunPullRequest(gitHubContext);
              }

              return true;
            }
            catch (Exception ex)
            {
                Trace.Error("Caught exception in JobPassesSecurityRestrictions");
                Trace.Error("As a safety precaution we are not allowing this job to run");
                Trace.Error(ex);
                return false;
            }
        }

        private bool OkayToRunPullRequest(GitHubContext gitHubContext)
        {
            var configStore = HostContext.GetService<IConfigurationStore>();
            var settings = configStore.GetSettings();
            var prSecuritySettings = settings.PullRequestSecuritySettings;

            if (prSecuritySettings is null) {
                Trace.Info("No pullRequestSecurity defined in settings, allowing this build");
                return true;
            }

            var githubEvent = gitHubContext["event"] as DictionaryContextData;
            var prData = githubEvent["pull_request"] as DictionaryContextData;

            var authorAssociation = prData.TryGetValue("author_association", out var value)
              ? value as StringContextData : null;


            // TODO: Allow COLLABORATOR, MEMBER too -- possibly by a config setting
            if (authorAssociation == "OWNER")
            {
                Trace.Info("PR is opened by a repo owner, always allowed");
                return true;
            }
            else if (prSecuritySettings.AllowContributors && authorAssociation == "COLLABORATOR") {
                Trace.Info("PR is from the repo collaborator, allowing");
                return true;
            }

            // pull_request.user.login is the user who opened the PR. Always fixed
            // Actor is the user who performed the push/open/close. For
            // predictability we validate based on the PR opener, so we don't
            // get surprised when re-opening an issue causes it to run on
            // self-hosted runners.
            var prUser = prData["user"] as DictionaryContextData;

            if (prUser == null)
            {
                Trace.Info("Unable to get PR user, not allowing PR to run");
                return false;
            }

            var login = prUser.TryGetValue("login", out value)
              ? value as StringContextData : null;

            Trace.Info($"GitHub PR.user.login is {login as StringContextData}");

            if (login == null)
            {
                Trace.Info("Unable to get PR actor, not allowing PR to run");
                return false;
            }

            if (prSecuritySettings.AllowedAuthors.Contains(login))
            {
                Trace.Info("Author in PR allowed list");
                return true;
            }
            else
            {
                Trace.Info($"Not running job as user ({login}) is not in {{{string.Join(", ", prSecuritySettings.AllowedAuthors)}}}");

                return false;
            }
        }

        private async Task<TaskResult> CompleteJobAsync(IJobServer jobServer, IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, TaskResult? taskResult = null)
        {
            jobContext.Debug($"Finishing: {message.JobDisplayName}");
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

            if (!jobContext.Global.Features.HasFlag(PlanFeatures.JobCompletedPlanEvent))
            {
                Trace.Info($"Skip raise job completed event call from worker because Plan version is {message.Plan.Version}");
                return result;
            }

            Trace.Info("Raising job completed event.");
            var jobCompletedEvent = new JobCompletedEvent(message.RequestId, message.JobId, result, jobContext.JobOutputs, jobContext.ActionsEnvironment);

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
                catch (TaskOrchestrationPlanTerminatedException ex)
                {
                    Trace.Error($"TaskOrchestrationPlanTerminatedException received, while attempting to raise JobCompletedEvent for job {message.JobId}.");
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
    }
}
