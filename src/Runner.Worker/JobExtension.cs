using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common.Util;
using System.Linq;
using System.Diagnostics;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.IO;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(JobExtension))]

    public interface IJobExtension : IRunnerService
    {
        Task<List<IStep>> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message);
        Task FinalizeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, DateTime jobStartTimeUtc);
    }

    public sealed class JobExtension : RunnerService, IJobExtension
    {
        private readonly HashSet<string> _existingProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _processCleanup;
        private string _processLookupId = $"github_{Guid.NewGuid()}";

        // download all required actions.
        // make sure all step condition inputs are valid.
        // build up three list of steps for jobrunner. (pre-job, job, post-job)
        public async Task<List<IStep>> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message)
        {
            Trace.Entering();
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNull(message, nameof(message));

            // create a new timeline record node for 'Set up job'
            IExecutionContext context = jobContext.CreateChild(Guid.NewGuid(), "Set up job", $"{nameof(JobExtension)}_Init", null, null);

            List<IStep> preJobSteps = new List<IStep>();
            List<IStep> jobSteps = new List<IStep>();
            List<IStep> postJobSteps = new List<IStep>();
            using (var register = jobContext.CancellationToken.Register(() => { context.CancelToken(); }))
            {
                try
                {
                    context.Start();
                    context.Debug($"Starting: Set up job");

                    // Set agent version variable.
                    context.Output($"Current runner version: '{BuildConstants.RunnerPackage.Version}'");

                    // Print proxy setting information for better diagnostic experience
                    var runnerWebProxy = HostContext.GetService<IRunnerWebProxy>();
                    if (!string.IsNullOrEmpty(runnerWebProxy.ProxyAddress))
                    {
                        context.Output($"Runner is running behind proxy server: '{runnerWebProxy.ProxyAddress}'");
                    }

                    // Give job extension a chance to initialize
                    Trace.Info($"Prepare workflow directory");
                    // We only support checkout one repository at this time.
                    var repoFullName = context.GetGitHubContext("repository");
                    ArgUtil.NotNull(repoFullName, nameof(repoFullName));
                    context.Debug($"Primary repository: {repoFullName}");

                    // Prepare the workflow directory.
                    context.Output("Prepare workflow directory");
                    var directoryManager = HostContext.GetService<IPipelineDirectoryManager>();
                    TrackingConfig trackingConfig = directoryManager.PrepareDirectory(
                        context,
                        message.Workspace);

                    // Set the directory variables.
                    context.Debug("Update context data");
                    string _workDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                    context.SetRunnerContext("workspace", Path.Combine(_workDirectory, trackingConfig.PipelineDirectory));
                    context.SetGitHubContext("workspace", Path.Combine(_workDirectory, trackingConfig.WorkspaceDirectory));

                    // Parse all actions' conditions.
                    Trace.Info("Parsing all step condition inputs.");
                    var expression = HostContext.GetService<IExpressionManager>();
                    Dictionary<Guid, IExpressionNode> actionConditionMap = new Dictionary<Guid, IExpressionNode>();
                    foreach (var step in message.Steps)
                    {
                        IExpressionNode condition;
                        if (string.IsNullOrEmpty(step.Condition))
                        {
                            condition = ExpressionManager.Succeeded;
                        }
                        else
                        {
                            context.Debug($"Step '{step.DisplayName}' has following condition: '{step.Condition}'.");
                            if (step.Type == Pipelines.StepType.Action)
                            {
                                condition = expression.Parse(context, step.Condition);
                            }
                            else
                            {
                                throw new NotSupportedException(step.Type.ToString());
                            }
                        }

                        actionConditionMap[step.Id] = condition;
                    }

                    // build up 3 lists of steps, pre-job, job, post-job
                    Stack<IStep> postJobStepsBuilder = new Stack<IStep>();

                    // Download actions if not already in the cache
                    Trace.Info("Downloading actions.");
                    var actionManager = HostContext.GetService<IActionManager>();
                    var prepareSteps = await actionManager.PrepareActionsAsync(context, message.Steps);
                    preJobSteps.AddRange(prepareSteps);

                    if (context.Container != null || context.SidecarContainers.Count > 0)
                    {
                        var containerProvider = HostContext.GetService<IContainerOperationProvider>();
                        var containers = new List<Container.ContainerInfo>();
                        if (context.Container != null)
                        {
                            containers.Add(context.Container);
                        }
                        containers.AddRange(context.SidecarContainers);

                        preJobSteps.Add(new JobExtensionRunner(runAsync: containerProvider.StartContainersAsync,
                                                                          condition: ExpressionManager.Succeeded,
                                                                          displayName: "Initialize containers",
                                                                          data: (object)containers));
                        postJobStepsBuilder.Push(new JobExtensionRunner(runAsync: containerProvider.StopContainersAsync,
                                                                        condition: ExpressionManager.Always,
                                                                        displayName: "Stop containers",
                                                                        data: (object)containers));
                    }

                    foreach (var step in message.Steps)
                    {
                        if (step.Type == Pipelines.StepType.Action)
                        {
                            var action = step as Pipelines.ActionStep;
                            Trace.Info($"Adding {action.DisplayName}.");
                            var actionRunner = HostContext.CreateService<IActionRunner>();
                            actionRunner.Action = action;
                            actionRunner.Condition = actionConditionMap[action.Id];
                            jobSteps.Add(actionRunner);
                        }
                    }

                    // create execution context for all pre-job steps
                    foreach (var step in preJobSteps)
                    {
                        if (step is JobExtensionRunner)
                        {
                            JobExtensionRunner extensionStep = step as JobExtensionRunner;
                            ArgUtil.NotNull(extensionStep, extensionStep.DisplayName);
                            Guid stepId = Guid.NewGuid();
                            extensionStep.ExecutionContext = jobContext.CreateChild(stepId, extensionStep.DisplayName, null, null, stepId.ToString("N"));
                        }
                    }

                    // create task execution context for all job steps from task
                    foreach (var step in jobSteps)
                    {
                        if (step is IActionRunner actionStep)
                        {
                            ArgUtil.NotNull(actionStep, step.DisplayName);
                            actionStep.ExecutionContext = jobContext.CreateChild(actionStep.Action.Id, actionStep.DisplayName, actionStep.Action.Name, actionStep.Action.ScopeName, actionStep.Action.ContextName, outputForward: true);
                        }
                    }

                    // Add post-job steps from Tasks
                    Trace.Info("Adding post-job steps from tasks.");
                    while (postJobStepsBuilder.Count > 0)
                    {
                        postJobSteps.Add(postJobStepsBuilder.Pop());
                    }

                    // create task execution context for all post-job steps from task
                    foreach (var step in postJobSteps)
                    {
                        if (step is JobExtensionRunner)
                        {
                            JobExtensionRunner extensionStep = step as JobExtensionRunner;
                            ArgUtil.NotNull(extensionStep, extensionStep.DisplayName);
                            Guid stepId = Guid.NewGuid();
                            extensionStep.ExecutionContext = jobContext.CreateChild(stepId, extensionStep.DisplayName, stepId.ToString("N"), null, null);
                        }
                    }

                    List<IStep> steps = new List<IStep>();
                    steps.AddRange(preJobSteps);
                    steps.AddRange(jobSteps);
                    steps.AddRange(postJobSteps);

                    // Start agent log plugin host process
                    // var logPlugin = HostContext.GetService<IAgentLogPlugin>();
                    // await logPlugin.StartAsync(context, steps, jobContext.CancellationToken);

                    // Prepare for orphan process cleanup
                    _processCleanup = jobContext.Variables.GetBoolean("process.clean") ?? true;
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

        public async Task FinalizeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message, DateTime jobStartTimeUtc)
        {
            Trace.Entering();
            ArgUtil.NotNull(jobContext, nameof(jobContext));

            // create a new timeline record node for 'Finalize job'
            IExecutionContext context = jobContext.CreateChild(Guid.NewGuid(), "Complete job", $"{nameof(JobExtension)}_Final", null, null);
            using (var register = jobContext.CancellationToken.Register(() => { context.CancelToken(); }))
            {
                try
                {
                    context.Start();
                    context.Debug("Starting: Complete job");

                    // Wait for agent log plugin process exits
                    // var logPlugin = HostContext.GetService<IAgentLogPlugin>();
                    // try
                    // {
                    //     await logPlugin.WaitAsync(context);
                    // }
                    // catch (Exception ex)
                    // {
                    //     // Log and ignore the error from log plugin finalization.
                    //     Trace.Error($"Caught exception from log plugin finalization: {ex}");
                    //     context.Output(ex.Message);
                    // }

                    if (context.Variables.GetBoolean(Constants.Variables.Actions.RunnerDebug) ?? false)
                    {
                        Trace.Info("Support log upload starting.");
                        context.Output("Uploading runner diagnostic logs");

                        IDiagnosticLogManager diagnosticLogManager = HostContext.GetService<IDiagnosticLogManager>();

                        try
                        {
                            await diagnosticLogManager.UploadDiagnosticLogsAsync(executionContext: context, parentContext: jobContext, message: message, jobStartTimeUtc: jobStartTimeUtc);

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
