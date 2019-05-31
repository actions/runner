// workflow:
//   build:
//     actions:
//     - run: printenv
//       name: printenv_name
//       id: printenv_id
//       if: succeeded()
//       env:
//         foo: bar
//     - uses: 'docker://ubuntu:16.04'
//       if: succeeded()
//       with:
//         entryPoint: /bin/bash
//         args: echo
//       env:
//         foo: bar
//     - uses: actions/npm@master
//       name: npm_version
//       with:
//         args: version
//     - uses: actions/aws/cli@master
//       id: cli_id
//       with:
//         args: version


using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
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
using System.Text;
using System.IO.Compression;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating.Tokens;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(JobRunner))]
    public interface IJobRunner : IAgentService
    {
        Task<TaskResult> RunAsync(Pipelines.AgentJobRequestMessage message, CancellationToken jobRequestCancellationToken);
    }

    public sealed class JobRunner : AgentService, IJobRunner
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

            // back compat TfsServerUrl
            message.Variables[Constants.Variables.System.TFServerUrl] = systemConnection.Url.AbsoluteUri;

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

            _jobServerQueue.Start(message);
            HostContext.WritePerfCounter($"WorkerJobServerQueueStarted_{message.RequestId.ToString()}");

            MakeJobMessageCompat(message);

            IExecutionContext jobContext = null;
            CancellationTokenRegistration? agentShutdownRegistration = null;
            try
            {
                // Create the job execution context.
                jobContext = HostContext.CreateService<IExecutionContext>();
                jobContext.InitializeJob(message, jobRequestCancellationToken);
                Trace.Info("Starting the job execution context.");
                jobContext.Start();
                jobContext.Section(StringUtil.Loc("StepStarting", message.JobDisplayName));

                agentShutdownRegistration = HostContext.AgentShutdownToken.Register(() =>
                {
                    // log an issue, then agent get shutdown by Ctrl-C or Ctrl-Break.
                    // the server will use Ctrl-Break to tells the agent that operating system is shutting down.
                    string errorMessage;
                    switch (HostContext.AgentShutdownReason)
                    {
                        case ShutdownReason.UserCancelled:
                            errorMessage = StringUtil.Loc("UserShutdownAgent");
                            break;
                        case ShutdownReason.OperatingSystemShutdown:
                            errorMessage = StringUtil.Loc("OperatingSystemShutdown", Environment.MachineName);
                            break;
                        default:
                            throw new ArgumentException(HostContext.AgentShutdownReason.ToString(), nameof(HostContext.AgentShutdownReason));
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
                jobContext.SetRunnerContext("version", BuildConstants.AgentPackage.Version);

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
                IJobExtension jobExtension = new BuildJobExtension();
                jobExtension.Initialize(HostContext);
                ArgUtil.NotNull(jobExtension, nameof(jobExtension));

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
                if (agentShutdownRegistration != null)
                {
                    agentShutdownRegistration.Value.Dispose();
                    agentShutdownRegistration = null;
                }

                await ShutdownQueue(throwOnFailure: false);
            }
        }

        private void MakeJobMessageCompat(Pipelines.AgentJobRequestMessage message)
        {
            List<Pipelines.JobStep> steps = new List<Pipelines.JobStep>();
            foreach (var step in message.Steps)
            {
                if (step.Type == Pipelines.StepType.Action)
                {
                    var action = step as Pipelines.ActionStep;
                    if (action.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry)
                    {
                        var containerReference = action.Reference as Pipelines.ContainerRegistryReference;
                        if (!string.IsNullOrEmpty(containerReference.Container))
                        {
                            // compat mode, convert container resource to inline step inputs
                            var containerResource = message.Resources.Containers.FirstOrDefault(x => x.Alias == containerReference.Container);
                            ArgUtil.NotNull(containerResource, nameof(containerResource));

                            containerReference.Image = containerResource.Image;
                        }
                    }
                    else if (action.Reference.Type == Pipelines.ActionSourceType.Repository)
                    {
                        var repoReference = action.Reference as Pipelines.RepositoryPathReference;
                        if (!string.IsNullOrEmpty(repoReference.Repository))
                        {
                            // compat mode, convert repository resource to inline step inputs
                            var repoResource = message.Resources.Repositories.FirstOrDefault(x => x.Alias == repoReference.Repository);
                            ArgUtil.NotNull(repoResource, nameof(repoResource));

                            repoReference.Name = repoResource.Id;
                            repoReference.Ref = repoResource.Version;
                            if (repoResource.Alias == Pipelines.PipelineConstants.SelfAlias)
                            {
                                repoReference.RepositoryType = Pipelines.PipelineConstants.SelfAlias;
                            }
                            else
                            {
                                repoReference.RepositoryType = repoResource.Type;
                            }
                        }
                    }

                    steps.Add(action);
                }
                else if (step.Type == Pipelines.StepType.Task)
                {
                    var task = step as Pipelines.TaskStep;
                    if (task.Reference.Id == Pipelines.PipelineConstants.CheckoutTask.Id)
                    {
                        var checkoutAction = new Pipelines.ActionStep
                        {
                            Id = task.Id,
                            Name = task.Name,
                            DisplayName = task.DisplayName,
                            Enabled = true,
                            Reference = new Pipelines.PluginReference
                            {
                                Plugin = Pipelines.PipelineConstants.AgentPlugins.Checkout
                            }
                        };
                        var inputs = new MappingToken(null, null, null);
                        inputs.Add(new LiteralToken(null, null, null, Pipelines.PipelineConstants.CheckoutTaskInputs.Repository), new LiteralToken(null, null, null, Pipelines.PipelineConstants.SelfAlias));
                        checkoutAction.Inputs = inputs;

                        steps.Add(checkoutAction);
                    }
                    else
                    {
                        throw new InvalidOperationException(task.Name);
                    }
                }
                else if (step.Type == Pipelines.StepType.Script)
                {
                    var script = step as Pipelines.ScriptStep;
                    var result = new Pipelines.ActionStep
                    {
                        Enabled = true,
                        Id = script.Id,
                        Name = script.Name,
                        DisplayName = script.DisplayName,
                        Condition = script.Condition,
                        TimeoutInMinutes = script.TimeoutInMinutes,
                        Environment = script.Environment?.Clone(true),
                        Reference = new Pipelines.ScriptReference(),
                        Inputs = script.Inputs?.Clone(true),
                    };
                    steps.Add(script);
                }
            }

            message.Steps.Clear();
            message.Steps.AddRange(steps);
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
            AgentSettings settings = HostContext.GetService<IConfigurationStore>().GetSettings();
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
    }
}
