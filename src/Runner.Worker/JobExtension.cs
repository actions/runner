using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker
{
    [DataContract]
    public class SetupInfo
    {
        [DataMember]
        public string Group { get; set; }

        [DataMember]
        public string Detail { get; set; }
    }

    [ServiceLocator(Default = typeof(JobExtension))]

    public interface IJobExtension : IRunnerService
    {
        Task<List<IStep>> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message);
        void FinalizeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, DateTime jobStartTimeUtc);
    }

    public sealed class JobExtension : RunnerService, IJobExtension
    {
        private readonly HashSet<string> _existingProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _processCleanup;
        private string _processLookupId = $"github_{Guid.NewGuid()}";
        private CancellationTokenSource _diskSpaceCheckToken = new CancellationTokenSource();
        private Task _diskSpaceCheckTask = null;

        // Download all required actions.
        // Make sure all condition inputs are valid.
        // Build up three list of steps for jobrunner (pre-job, job, post-job).
        public async Task<List<IStep>> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message)
        {
            Trace.Entering();
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNull(message, nameof(message));

            // Create a new timeline record for 'Set up job'
            IExecutionContext context = jobContext.CreateChild(Guid.NewGuid(), "Set up job", $"{nameof(JobExtension)}_Init", null, null, ActionRunStage.Pre);
            context.StepTelemetry.Type = "runner";
            context.StepTelemetry.Action = "setup_job";

            List<IStep> preJobSteps = new List<IStep>();
            List<IStep> jobSteps = new List<IStep>();
            using (var register = jobContext.CancellationToken.Register(() => { context.CancelToken(); }))
            {
                try
                {
                    context.Start();
                    context.Debug($"Starting: Set up job");
                    context.Output($"Current runner version: '{BuildConstants.RunnerPackage.Version}'");

                    var setting = HostContext.GetService<IConfigurationStore>().GetSettings();
                    var credFile = HostContext.GetConfigFile(WellKnownConfigFile.Credentials);
                    if (File.Exists(credFile))
                    {
                        var credData = IOUtil.LoadObject<CredentialData>(credFile);
                        if (credData != null &&
                            credData.Data.TryGetValue("clientId", out var clientId))
                        {
                            // print out HostName for self-hosted runner
                            context.Output($"Runner name: '{setting.AgentName}'");
                            if (message.Variables.TryGetValue("system.runnerGroupName", out VariableValue runnerGroupName))
                            {
                                context.Output($"Runner group name: '{runnerGroupName.Value}'");
                            }
                            context.Output($"Machine name: '{Environment.MachineName}'");
                        }
                    }

                    var setupInfoFile = HostContext.GetConfigFile(WellKnownConfigFile.SetupInfo);
                    if (File.Exists(setupInfoFile))
                    {
                        Trace.Info($"Load machine setup info from {setupInfoFile}");
                        try
                        {
                            var setupInfo = IOUtil.LoadObject<List<SetupInfo>>(setupInfoFile);
                            if (setupInfo?.Count > 0)
                            {
                                foreach (var info in setupInfo)
                                {
                                    if (!string.IsNullOrEmpty(info?.Detail))
                                    {
                                        var groupName = info.Group;
                                        if (string.IsNullOrEmpty(groupName))
                                        {
                                            groupName = "Machine Setup Info";
                                        }

                                        context.Output($"##[group]{groupName}");
                                        var multiLines = info.Detail.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
                                        foreach (var line in multiLines)
                                        {
                                            context.Output(line);
                                        }
                                        context.Output("##[endgroup]");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Output($"Fail to load and print machine setup info: {ex.Message}");
                            Trace.Error(ex);
                        }
                    }

                    try
                    {
                        var tokenPermissions = jobContext.Global.Variables.Get("system.github.token.permissions") ?? "";
                        if (!string.IsNullOrEmpty(tokenPermissions))
                        {
                            context.Output($"##[group]GITHUB_TOKEN Permissions");
                            var permissions = StringUtil.ConvertFromJson<Dictionary<string, string>>(tokenPermissions);
                            foreach (KeyValuePair<string, string> entry in permissions)
                            {
                                context.Output($"{entry.Key}: {entry.Value}");
                            }
                            context.Output("##[endgroup]");
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Output($"Fail to parse and display GITHUB_TOKEN permissions list: {ex.Message}");
                        Trace.Error(ex);
                    }

                    var secretSource = context.GetGitHubContext("secret_source");
                    if (!string.IsNullOrEmpty(secretSource))
                    {
                        context.Output($"Secret source: {secretSource}");
                    }

                    var repoFullName = context.GetGitHubContext("repository");
                    ArgUtil.NotNull(repoFullName, nameof(repoFullName));
                    context.Debug($"Primary repository: {repoFullName}");

                    // Print proxy setting information for better diagnostic experience
                    if (!string.IsNullOrEmpty(HostContext.WebProxy.HttpProxyAddress))
                    {
                        context.Output($"Runner is running behind proxy server '{HostContext.WebProxy.HttpProxyAddress}' for all HTTP requests.");
                    }
                    if (!string.IsNullOrEmpty(HostContext.WebProxy.HttpsProxyAddress))
                    {
                        context.Output($"Runner is running behind proxy server '{HostContext.WebProxy.HttpsProxyAddress}' for all HTTPS requests.");
                    }

                    // Prepare the workflow directory
                    context.Output("Prepare workflow directory");
                    var directoryManager = HostContext.GetService<IPipelineDirectoryManager>();
                    TrackingConfig trackingConfig = directoryManager.PrepareDirectory(
                        context,
                        message.Workspace);

                    // Set the directory variables
                    context.Debug("Update context data");
                    string _workDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                    context.SetRunnerContext("workspace", Path.Combine(_workDirectory, trackingConfig.PipelineDirectory));
                    context.SetGitHubContext("workspace", Path.Combine(_workDirectory, trackingConfig.WorkspaceDirectory));

                    // Temporary hack for GHES alpha
                    var configurationStore = HostContext.GetService<IConfigurationStore>();
                    var runnerSettings = configurationStore.GetSettings();
                    if (string.IsNullOrEmpty(context.GetGitHubContext("server_url")) && !runnerSettings.IsHostedServer && !string.IsNullOrEmpty(runnerSettings.GitHubUrl))
                    {
                        var url = new Uri(runnerSettings.GitHubUrl);
                        var portInfo = url.IsDefaultPort ? string.Empty : $":{url.Port.ToString(CultureInfo.InvariantCulture)}";
                        context.SetGitHubContext("server_url", $"{url.Scheme}://{url.Host}{portInfo}");
                        context.SetGitHubContext("api_url", $"{url.Scheme}://{url.Host}{portInfo}/api/v3");
                        context.SetGitHubContext("graphql_url", $"{url.Scheme}://{url.Host}{portInfo}/api/graphql");
                    }

                    // Evaluate the job-level environment variables
                    context.Debug("Evaluating job-level environment variables");
                    var templateEvaluator = context.ToPipelineTemplateEvaluator();
                    foreach (var token in message.EnvironmentVariables)
                    {
                        var environmentVariables = templateEvaluator.EvaluateStepEnvironment(token, jobContext.ExpressionValues, jobContext.ExpressionFunctions, VarUtil.EnvironmentVariableKeyComparer);
                        foreach (var pair in environmentVariables)
                        {
                            context.Global.EnvironmentVariables[pair.Key] = pair.Value ?? string.Empty;
                            context.SetEnvContext(pair.Key, pair.Value ?? string.Empty);
                        }
                    }

                    // Evaluate the job container
                    context.Debug("Evaluating job container");
                    var container = templateEvaluator.EvaluateJobContainer(message.JobContainer, jobContext.ExpressionValues, jobContext.ExpressionFunctions);
                    if (container != null)
                    {
                        jobContext.Global.Container = new Container.ContainerInfo(HostContext, container);
                    }

                    // Evaluate the job service containers
                    context.Debug("Evaluating job service containers");
                    var serviceContainers = templateEvaluator.EvaluateJobServiceContainers(message.JobServiceContainers, jobContext.ExpressionValues, jobContext.ExpressionFunctions);
                    if (serviceContainers?.Count > 0)
                    {
                        foreach (var pair in serviceContainers)
                        {
                            var networkAlias = pair.Key;
                            var serviceContainer = pair.Value;
                            jobContext.Global.ServiceContainers.Add(new Container.ContainerInfo(HostContext, serviceContainer, false, networkAlias));
                        }
                    }

                    // Evaluate the job defaults
                    context.Debug("Evaluating job defaults");
                    foreach (var token in message.Defaults)
                    {
                        var defaults = token.AssertMapping("defaults");
                        if (defaults.Any(x => string.Equals(x.Key.AssertString("defaults key").Value, "run", StringComparison.OrdinalIgnoreCase)))
                        {
                            context.Global.JobDefaults["run"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            var defaultsRun = defaults.First(x => string.Equals(x.Key.AssertString("defaults key").Value, "run", StringComparison.OrdinalIgnoreCase));
                            var jobDefaults = templateEvaluator.EvaluateJobDefaultsRun(defaultsRun.Value, jobContext.ExpressionValues, jobContext.ExpressionFunctions);
                            foreach (var pair in jobDefaults)
                            {
                                if (!string.IsNullOrEmpty(pair.Value))
                                {
                                    context.Global.JobDefaults["run"][pair.Key] = pair.Value;
                                }
                            }
                        }
                    }

                    // Build up 2 lists of steps, pre-job, job
                    // Download actions not already in the cache
                    Trace.Info("Downloading actions");
                    var actionManager = HostContext.GetService<IActionManager>();
                    var prepareResult = await actionManager.PrepareActionsAsync(context, message.Steps);

                    // add hook to preJobSteps
                    var startedHookPath = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_HOOK_JOB_STARTED");
                    if (!string.IsNullOrEmpty(startedHookPath))
                    {
                        var hookProvider = HostContext.GetService<IJobHookProvider>();
                        var jobHookData = new JobHookData(ActionRunStage.Pre, startedHookPath, "Set up runner");
                        preJobSteps.Add(new JobExtensionRunner(runAsync: hookProvider.RunHook,
                                                                          condition: $"{PipelineTemplateConstants.Always}()",
                                                                          displayName: "Set up runner",
                                                                          data: (object)jobHookData));
                    }

                    preJobSteps.AddRange(prepareResult.ContainerSetupSteps);

                    // Add start-container steps, record and stop-container steps
                    if (jobContext.Global.Container != null || jobContext.Global.ServiceContainers.Count > 0)
                    {
                        var containerProvider = HostContext.GetService<IContainerOperationProvider>();
                        var containers = new List<Container.ContainerInfo>();
                        if (jobContext.Global.Container != null)
                        {
                            containers.Add(jobContext.Global.Container);
                        }
                        containers.AddRange(jobContext.Global.ServiceContainers);

                        preJobSteps.Add(new JobExtensionRunner(runAsync: containerProvider.StartContainersAsync,
                                                                          condition: $"{PipelineTemplateConstants.Success}()",
                                                                          displayName: "Initialize containers",
                                                                          data: (object)containers));
                    }

                    // Add action steps
                    foreach (var step in message.Steps)
                    {
                        if (step.Type == Pipelines.StepType.Action)
                        {
                            var action = step as Pipelines.ActionStep;
                            Trace.Info($"Adding {action.DisplayName}.");
                            var actionRunner = HostContext.CreateService<IActionRunner>();
                            actionRunner.Action = action;
                            actionRunner.Stage = ActionRunStage.Main;
                            actionRunner.Condition = step.Condition;
                            var contextData = new Pipelines.ContextData.DictionaryContextData();
                            if (message.ContextData?.Count > 0)
                            {
                                foreach (var pair in message.ContextData)
                                {
                                    contextData[pair.Key] = pair.Value;
                                }
                            }

                            actionRunner.TryEvaluateDisplayName(contextData, context);
                            jobSteps.Add(actionRunner);

                            if (prepareResult.PreStepTracker.TryGetValue(step.Id, out var preStep))
                            {
                                Trace.Info($"Adding pre-{action.DisplayName}.");
                                preStep.TryEvaluateDisplayName(contextData, context);
                                preStep.DisplayName = $"Pre {preStep.DisplayName}";
                                preJobSteps.Add(preStep);
                            }
                        }
                    }

                    var intraActionStates = new Dictionary<Guid, Dictionary<string, string>>();
                    foreach (var preStep in prepareResult.PreStepTracker)
                    {
                        intraActionStates[preStep.Key] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    // Create execution context for pre-job steps
                    foreach (var step in preJobSteps)
                    {
                        if (step is JobExtensionRunner)
                        {
                            JobExtensionRunner extensionStep = step as JobExtensionRunner;
                            ArgUtil.NotNull(extensionStep, extensionStep.DisplayName);
                            Guid stepId = Guid.NewGuid();
                            extensionStep.ExecutionContext = jobContext.CreateChild(stepId, extensionStep.DisplayName, stepId.ToString("N"), null, stepId.ToString("N"), ActionRunStage.Pre);
                            extensionStep.ExecutionContext.StepTelemetry.Type = "runner";
                            extensionStep.ExecutionContext.StepTelemetry.Action = extensionStep.DisplayName.ToLowerInvariant().Replace(' ', '_');
                        }
                        else if (step is IActionRunner actionStep)
                        {
                            ArgUtil.NotNull(actionStep, step.DisplayName);
                            Guid stepId = Guid.NewGuid();
                            actionStep.ExecutionContext = jobContext.CreateChild(stepId, actionStep.DisplayName, stepId.ToString("N"), null, null, ActionRunStage.Pre, intraActionStates[actionStep.Action.Id]);
                        }
                        else if (step is ManagedScriptStep)
                        {
                            var managedScriptStep = step as ManagedScriptStep;
                            managedScriptStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), step.DisplayName, $"{nameof(JobExtension)}_Set_up_runner", null, null, ActionRunStage.Pre);
                        }
                    }

                    // Create execution context for job steps
                    foreach (var step in jobSteps)
                    {
                        if (step is IActionRunner actionStep)
                        {
                            ArgUtil.NotNull(actionStep, step.DisplayName);
                            intraActionStates.TryGetValue(actionStep.Action.Id, out var intraActionState);
                            actionStep.ExecutionContext = jobContext.CreateChild(actionStep.Action.Id, actionStep.DisplayName, actionStep.Action.Name, null, actionStep.Action.ContextName, ActionRunStage.Main, intraActionState);
                        }
                    }

                    var completedHookPath = Environment.GetEnvironmentVariable("ACTIONS_RUNNER_HOOK_JOB_COMPLETED");
                    if (!string.IsNullOrEmpty(completedHookPath))
                    {
                        var hookProvider = HostContext.GetService<IJobHookProvider>();
                        var jobHookData = new JobHookData(ActionRunStage.Post, startedHookPath, "Complete runner");
                        jobContext.RegisterPostJobStep(new JobExtensionRunner(runAsync: hookProvider.RunHook,
                                                                          condition: $"{PipelineTemplateConstants.Always}()",
                                                                          displayName: "Complete runner",
                                                                          data: (object)jobHookData));
                    }

                    List<IStep> steps = new List<IStep>();
                    steps.AddRange(preJobSteps);
                    steps.AddRange(jobSteps);

                    // Prepare for orphan process cleanup
                    _processCleanup = jobContext.Global.Variables.GetBoolean("process.clean") ?? true;
                    if (_processCleanup)
                    {
                        // Set the RUNNER_TRACKING_ID env variable.
                        Environment.SetEnvironmentVariable(Constants.ProcessTrackingId, _processLookupId);
                        context.Debug("Collect running processes for tracking orphan processes.");

                        // Take a snapshot of current running processes
                        Dictionary<int, Process> processes = SnapshotProcesses();
                        foreach (var proc in processes)
                        {
                            // Pid_ProcessName
                            _existingProcesses.Add($"{proc.Key}_{proc.Value.ProcessName}");
                        }
                    }

                    jobContext.Global.EnvironmentVariables.TryGetValue(Constants.Runner.Features.DiskSpaceWarning, out var enableWarning);
                    if (StringUtil.ConvertToBoolean(enableWarning, defaultValue: true))
                    {
                        _diskSpaceCheckTask = CheckDiskSpaceAsync(context, _diskSpaceCheckToken.Token);
                    }

                    return steps;
                }
                catch (OperationCanceledException ex) when (jobContext.CancellationToken.IsCancellationRequested)
                {
                    // Log the exception and cancel the JobExtension Initialization.
                    Trace.Error($"Caught cancellation exception from JobExtension Initialization: {ex}");
                    context.Error(ex);
                    context.Result = TaskResult.Canceled;
                    throw;
                }
                catch (FailedToResolveActionDownloadInfoException ex)
                {
                    // Log the error and fail the JobExtension Initialization.
                    Trace.Error($"Caught exception from JobExtenion Initialization: {ex}");
                    context.InfrastructureError(ex.Message);
                    context.Result = TaskResult.Failed;
                    throw;
                }
                catch (Exception ex)
                {
                    // Log the error and fail the JobExtension Initialization.
                    Trace.Error($"Caught exception from JobExtension Initialization: {ex}");
                    context.Error(ex);
                    context.Result = TaskResult.Failed;
                    throw;
                }
                finally
                {
                    context.Debug("Finishing: Set up job");
                    context.Complete();
                }
            }
        }

        public void FinalizeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, DateTime jobStartTimeUtc)
        {
            Trace.Entering();
            ArgUtil.NotNull(jobContext, nameof(jobContext));

            // create a new timeline record node for 'Finalize job'
            IExecutionContext context = jobContext.CreateChild(Guid.NewGuid(), "Complete job", $"{nameof(JobExtension)}_Final", null, null, ActionRunStage.Post);
            context.StepTelemetry.Type = "runner";
            context.StepTelemetry.Action = "complete_joh";
            using (var register = jobContext.CancellationToken.Register(() => { context.CancelToken(); }))
            {
                try
                {
                    context.Start();
                    context.Debug("Starting: Complete job");

                    Trace.Info("Initialize Env context");

#if OS_WINDOWS
                    var envContext = new DictionaryContextData();
#else
                    var envContext = new CaseSensitiveDictionaryContextData();
#endif
                    context.ExpressionValues["env"] = envContext;
                    foreach (var pair in context.Global.EnvironmentVariables)
                    {
                        envContext[pair.Key] = new StringContextData(pair.Value ?? string.Empty);
                    }

                    // Populate env context for each step
                    Trace.Info("Initialize steps context");
                    context.ExpressionValues["steps"] = context.Global.StepsContext.GetScope(context.ScopeName);

                    var templateEvaluator = context.ToPipelineTemplateEvaluator();
                    // Evaluate job outputs
                    if (message.JobOutputs != null && message.JobOutputs.Type != TokenType.Null)
                    {
                        try
                        {
                            context.Output($"Evaluate and set job outputs");

                            // Populate env context for each step
                            Trace.Info("Initialize Env context for evaluating job outputs");

                            var outputs = templateEvaluator.EvaluateJobOutput(message.JobOutputs, context.ExpressionValues, context.ExpressionFunctions);
                            foreach (var output in outputs)
                            {
                                if (string.IsNullOrEmpty(output.Value))
                                {
                                    context.Debug($"Skip output '{output.Key}' since it's empty");
                                    continue;
                                }

                                if (!string.Equals(output.Value, HostContext.SecretMasker.MaskSecrets(output.Value)))
                                {
                                    context.Warning($"Skip output '{output.Key}' since it may contain secret.");
                                    continue;
                                }

                                context.Output($"Set output '{output.Key}'");
                                jobContext.JobOutputs[output.Key] = output.Value;
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Result = TaskResult.Failed;
                            context.Error($"Fail to evaluate job outputs");
                            context.Error(ex);
                            jobContext.Result = TaskResultUtil.MergeTaskResults(jobContext.Result, TaskResult.Failed);
                        }
                    }

                    // Evaluate environment data
                    if (jobContext.ActionsEnvironment?.Url != null && jobContext.ActionsEnvironment?.Url.Type != TokenType.Null)
                    {
                        try
                        {
                            context.Output($"Evaluate and set environment url");

                            var environmentUrlToken = templateEvaluator.EvaluateEnvironmentUrl(jobContext.ActionsEnvironment.Url, context.ExpressionValues, context.ExpressionFunctions);
                            var environmentUrl = environmentUrlToken.AssertString("environment.url");
                            if (!string.Equals(environmentUrl.Value, HostContext.SecretMasker.MaskSecrets(environmentUrl.Value)))
                            {
                                context.Warning($"Skip setting environment url as environment '{jobContext.ActionsEnvironment.Name}' may contain secret.");
                            }
                            else
                            {
                                context.Output($"Evaluated environment url: {environmentUrl}");
                                jobContext.ActionsEnvironment.Url = environmentUrlToken;
                            }
                        }
                        catch (Exception ex)
                        {
                            context.Result = TaskResult.Failed;
                            context.Error($"Failed to evaluate environment url");
                            context.Error(ex);
                            jobContext.Result = TaskResultUtil.MergeTaskResults(jobContext.Result, TaskResult.Failed);
                        }
                    }

                    if (context.Global.Variables.GetBoolean(Constants.Variables.Actions.RunnerDebug) ?? false)
                    {
                        Trace.Info("Support log upload starting.");
                        context.Output("Uploading runner diagnostic logs");

                        IDiagnosticLogManager diagnosticLogManager = HostContext.GetService<IDiagnosticLogManager>();

                        try
                        {
                            diagnosticLogManager.UploadDiagnosticLogs(executionContext: context, parentContext: jobContext, message: message, jobStartTimeUtc: jobStartTimeUtc);

                            Trace.Info("Support log upload complete.");
                            context.Output("Completed runner diagnostic log upload");
                        }
                        catch (Exception ex)
                        {
                            // Log the error but make sure we continue gracefully.
                            Trace.Info("Error uploading support logs.");
                            context.Output("Error uploading runner diagnostic logs");
                            Trace.Error(ex);
                        }
                    }

                    if (_processCleanup)
                    {
                        context.Output("Cleaning up orphan processes");

                        // Only check environment variable for any process that doesn't run before we invoke our process.
                        Dictionary<int, Process> currentProcesses = SnapshotProcesses();
                        foreach (var proc in currentProcesses)
                        {
                            if (proc.Key == Process.GetCurrentProcess().Id)
                            {
                                // skip for current process.
                                continue;
                            }

                            if (_existingProcesses.Contains($"{proc.Key}_{proc.Value.ProcessName}"))
                            {
                                Trace.Verbose($"Skip existing process. PID: {proc.Key} ({proc.Value.ProcessName})");
                            }
                            else
                            {
                                Trace.Info($"Inspecting process environment variables. PID: {proc.Key} ({proc.Value.ProcessName})");

                                string lookupId = null;
                                try
                                {
                                    lookupId = proc.Value.GetEnvironmentVariable(HostContext, Constants.ProcessTrackingId);
                                }
                                catch (Exception ex)
                                {
                                    Trace.Warning($"Ignore exception during read process environment variables: {ex.Message}");
                                    Trace.Verbose(ex.ToString());
                                }

                                if (string.Equals(lookupId, _processLookupId, StringComparison.OrdinalIgnoreCase))
                                {
                                    context.Output($"Terminate orphan process: pid ({proc.Key}) ({proc.Value.ProcessName})");
                                    try
                                    {
                                        proc.Value.Kill();
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.Error("Catch exception during orphan process cleanup.");
                                        Trace.Error(ex);
                                    }
                                }
                            }
                        }
                    }

                    if (_diskSpaceCheckTask != null)
                    {
                        _diskSpaceCheckToken.Cancel();
                    }
                }
                catch (Exception ex)
                {
                    // Log and ignore the error from JobExtension finalization.
                    Trace.Error($"Caught exception from JobExtension finalization: {ex}");
                    context.Output(ex.Message);
                }
                finally
                {
                    context.Debug("Finishing: Complete job");
                    context.Complete();
                }
            }
        }

        private async Task CheckDiskSpaceAsync(IExecutionContext context, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Add warning when disk is lower than system.runner.lowdiskspacethreshold from service (default to 100 MB on service side)
                var lowDiskSpaceThreshold = context.Global.Variables.GetInt(WellKnownDistributedTaskVariables.RunnerLowDiskspaceThreshold);
                if (lowDiskSpaceThreshold == null)
                {
                    Trace.Info($"Low diskspace warning is not enabled.");
                    return;
                }
                var workDirRoot = Directory.GetDirectoryRoot(HostContext.GetDirectory(WellKnownDirectory.Work));
                var driveInfo = new DriveInfo(workDirRoot);
                var freeSpaceInMB = driveInfo.AvailableFreeSpace / 1024 / 1024;
                if (freeSpaceInMB < lowDiskSpaceThreshold)
                {
                    var issue = new Issue() { Type = IssueType.Warning, Message = $"You are running out of disk space. The runner will stop working when the machine runs out of disk space. Free space left: {freeSpaceInMB} MB" };
                    issue.Data[Constants.Runner.InternalTelemetryIssueDataKey] = Constants.Runner.LowDiskSpace;
                    context.AddIssue(issue);
                    return;
                }

                try
                {
                    await Task.Delay(10 * 1000, token);
                }
                catch (TaskCanceledException)
                {
                    // ignore
                }
            }
        }

        private Dictionary<int, Process> SnapshotProcesses()
        {
            Dictionary<int, Process> snapshot = new Dictionary<int, Process>();
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    // On Windows, this will throw exception on error.
                    // On Linux, this will be NULL on error.
                    if (!string.IsNullOrEmpty(proc.ProcessName))
                    {
                        snapshot[proc.Id] = proc;
                    }
                }
                catch (Exception ex)
                {
                    Trace.Verbose($"Ignore any exception during taking process snapshot of process pid={proc.Id}: '{ex.Message}'.");
                }
            }

            Trace.Info($"Total accessible running process: {snapshot.Count}.");
            return snapshot;
        }
    }
}
