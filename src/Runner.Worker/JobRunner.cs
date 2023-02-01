using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;

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
        private RunnerSettings _runnerSettings;
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
            IRunnerService server = null;

            ServiceEndpoint systemConnection = message.Resources.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(message.MessageType, JobRequestMessageTypes.RunnerJobRequest, StringComparison.OrdinalIgnoreCase))
            {
                var runServer = HostContext.GetService<IRunServer>();
                VssCredentials jobServerCredential = VssUtil.GetVssCredential(systemConnection);
                await runServer.ConnectAsync(systemConnection.Url, jobServerCredential);
                server = runServer;
            }
            else 
            {
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
                server = jobServer;
            }
            

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
                    return await CompleteJobAsync(server, jobContext, message, TaskResult.Failed);
                }

                if (jobContext.Global.WriteDebug)
                {
                    jobContext.SetRunnerContext("debug", "1");
                }

                jobContext.SetRunnerContext("os", VarUtil.OS);
                jobContext.SetRunnerContext("arch", VarUtil.OSArchitecture);

                _runnerSettings = HostContext.GetService<IConfigurationStore>().GetSettings();
                jobContext.SetRunnerContext("name", _runnerSettings.AgentName);

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
                    // set the job to cancelled
                    // don't log error issue to job ExecutionContext, since server owns the job level issue
                    Trace.Error($"Job is cancelled during initialize.");
                    Trace.Error($"Caught exception: {ex}");
                    return await CompleteJobAsync(server, jobContext, message, TaskResult.Canceled);
                }
                catch (Exception ex)
                {
                    // set the job to failed.
                    // don't log error issue to job ExecutionContext, since server owns the job level issue
                    Trace.Error($"Job initialize failed.");
                    Trace.Error($"Caught exception from {nameof(jobExtension.InitializeJob)}: {ex}");
                    return await CompleteJobAsync(server, jobContext, message, TaskResult.Failed);
                }

                // trace out all steps
                Trace.Info($"Total job steps: {jobSteps.Count}.");
                Trace.Verbose($"Job steps: '{string.Join(", ", jobSteps.Select(x => x.DisplayName))}'");
                HostContext.WritePerfCounter($"WorkerJobInitialized_{message.RequestId.ToString()}");

                if (systemConnection.Data.TryGetValue("GenerateIdTokenUrl", out var generateIdTokenUrl) &&
                    !string.IsNullOrEmpty(generateIdTokenUrl))
                {
                    // Server won't issue ID_TOKEN for non-inprogress job.
                    // If the job is trying to use OIDC feature, we want the job to be marked as in-progress before running any customer's steps as much as we can.
                    // Timeline record update background process runs every 500ms, so delay 1000ms is enough for most of the cases
                    Trace.Info($"Waiting for job to be marked as started.");
                    await Task.WhenAny(_jobServerQueue.JobRecordUpdated.Task, Task.Delay(1000));
                }

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
                    return await CompleteJobAsync(server, jobContext, message, TaskResult.Failed);
                }
                finally
                {
                    Trace.Info("Finalize job.");
                    jobExtension.FinalizeJob(jobContext, message, jobStartTimeUtc);
                }

                Trace.Info($"Job result after all job steps finish: {jobContext.Result ?? TaskResult.Succeeded}");

                Trace.Info("Completing the job execution context.");
                return await CompleteJobAsync(server, jobContext, message);
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

        private async Task<TaskResult> CompleteJobAsync(IRunnerService server, IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, TaskResult? taskResult = null)
        {
            if (server is IRunServer runServer)
            {
                return await CompleteJobAsync(runServer, jobContext, message, taskResult);
            }
            else if (server is IJobServer jobServer)
            {
                return await CompleteJobAsync(jobServer, jobContext, message, taskResult);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private async Task<TaskResult> CompleteJobAsync(IRunServer runServer, IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, TaskResult? taskResult = null)
        {
            jobContext.Debug($"Finishing: {message.JobDisplayName}");
            TaskResult result = jobContext.Complete(taskResult);
            if (jobContext.Global.Variables.TryGetValue("Node12ActionsWarnings", out var node12Warnings))
            {
                var actions = string.Join(", ", StringUtil.ConvertFromJson<HashSet<string>>(node12Warnings));
                jobContext.Warning(string.Format(Constants.Runner.Node12DetectedAfterEndOfLife, actions));
            }

            // Clean TEMP after finish process jobserverqueue, since there might be a pending fileupload still use the TEMP dir.
            _tempDirectoryManager?.CleanupTempDirectory();

            // Load any upgrade telemetry
            LoadFromTelemetryFile(jobContext.Global.JobTelemetry);

            // Make sure we don't submit secrets as telemetry
            MaskTelemetrySecrets(jobContext.Global.JobTelemetry);

            Trace.Info($"Raising job completed against run service");
            var completeJobRetryLimit = 5;
            var exceptions = new List<Exception>();
            while (completeJobRetryLimit-- > 0)
            {
                try
                {
                    await runServer.CompleteJobAsync(message.Plan.PlanId, message.JobId, default);
                    return result;
                }
                catch (Exception ex)
                {
                    Trace.Error($"Catch exception while attempting to complete job {message.JobId}, job request {message.RequestId}.");
                    Trace.Error(ex);
                    exceptions.Add(ex);
                }

                // delay 5 seconds before next retry.
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            // rethrow exceptions from all attempts.
            throw new AggregateException(exceptions);
        }

        private async Task<TaskResult> CompleteJobAsync(IJobServer jobServer, IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, TaskResult? taskResult = null)
        {
            jobContext.Debug($"Finishing: {message.JobDisplayName}");
            TaskResult result = jobContext.Complete(taskResult);

            if (_runnerSettings.DisableUpdate == true)
            {
                try
                {
                    var currentVersion = new PackageVersion(BuildConstants.RunnerPackage.Version);
                    ServiceEndpoint systemConnection = message.Resources.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
                    VssCredentials serverCredential = VssUtil.GetVssCredential(systemConnection);

                    var runnerServer = HostContext.GetService<IRunnerServer>();
                    await runnerServer.ConnectAsync(systemConnection.Url, serverCredential);
                    var serverPackages = await runnerServer.GetPackagesAsync("agent", BuildConstants.RunnerPackage.PackageName, 5, includeToken: false, cancellationToken: CancellationToken.None);
                    if (serverPackages.Count > 0)
                    {
                        serverPackages = serverPackages.OrderByDescending(x => x.Version).ToList();
                        Trace.Info($"Newer packages {StringUtil.ConvertToJson(serverPackages.Select(x => x.Version.ToString()))}");

                        var warnOnFailedJob = false; // any minor/patch version behind.
                        var warnOnOldRunnerVersion = false; // >= 2 minor version behind
                        if (serverPackages.Any(x => x.Version.CompareTo(currentVersion) > 0))
                        {
                            Trace.Info($"Current runner version {currentVersion} is behind the latest runner version {serverPackages[0].Version}.");
                            warnOnFailedJob = true;
                        }

                        if (serverPackages.Where(x => x.Version.Major == currentVersion.Major && x.Version.Minor > currentVersion.Minor).Count() > 1)
                        {
                            Trace.Info($"Current runner version {currentVersion} is way behind the latest runner version {serverPackages[0].Version}.");
                            warnOnOldRunnerVersion = true;
                        }

                        if (result == TaskResult.Failed && warnOnFailedJob)
                        {
                            jobContext.Warning($"This job failure may be caused by using an out of date self-hosted runner. You are currently using runner version {currentVersion}. Please update to the latest version {serverPackages[0].Version}");
                        }
                        else if (warnOnOldRunnerVersion)
                        {
                            jobContext.Warning($"This self-hosted runner is currently using runner version {currentVersion}. This version is out of date. Please update to the latest version {serverPackages[0].Version}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Ignore any error since suggest runner update is best effort.
                    Trace.Error($"Caught exception during runner version check: {ex}");
                }
            }

            if (jobContext.Global.Variables.TryGetValue("Node12ActionsWarnings", out var node12Warnings))
            {
                var actions = string.Join(", ", StringUtil.ConvertFromJson<HashSet<string>>(node12Warnings));
                jobContext.Warning(string.Format(Constants.Runner.Node12DetectedAfterEndOfLife, actions));
            }

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

            // Load any upgrade telemetry
            LoadFromTelemetryFile(jobContext.Global.JobTelemetry);

            // Make sure we don't submit secrets as telemetry
            MaskTelemetrySecrets(jobContext.Global.JobTelemetry);

            Trace.Info($"Raising job completed event");
            var jobCompletedEvent = new JobCompletedEvent(message.RequestId, message.JobId, result, jobContext.JobOutputs, jobContext.ActionsEnvironment, jobContext.Global.StepsTelemetry, jobContext.Global.JobTelemetry);

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

        private void MaskTelemetrySecrets(List<JobTelemetry> jobTelemetry)
        {
            foreach (var telemetryItem in jobTelemetry)
            {
                telemetryItem.Message = HostContext.SecretMasker.MaskSecrets(telemetryItem.Message);
            }
        }

        private void LoadFromTelemetryFile(List<JobTelemetry> jobTelemetry)
        {
            try
            {
                var telemetryFilePath = HostContext.GetConfigFile(WellKnownConfigFile.Telemetry);
                if (File.Exists(telemetryFilePath))
                {
                    var telemetryData = File.ReadAllText(telemetryFilePath, Encoding.UTF8);
                    var telemetry = new JobTelemetry
                    {
                        Message = $"Runner File Telemetry:\n{telemetryData}",
                        Type = JobTelemetryType.General
                    };
                    jobTelemetry.Add(telemetry);
                    IOUtil.DeleteFile(telemetryFilePath);
                }
            }
            catch (Exception e)
            {
                Trace.Error("Error when trying to load telemetry from telemetry file");
                Trace.Error(e);
            }
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
