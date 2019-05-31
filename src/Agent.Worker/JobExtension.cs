using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.Linq;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IJobExtension : IExtension
    {
        Task<List<IStep>> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message);
        Task FinalizeJob(IExecutionContext jobContext);
    }

    public abstract class JobExtension : AgentService, IJobExtension
    {
        private readonly HashSet<string> _existingProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _processCleanup;
        private string _processLookupId = $"vsts_{Guid.NewGuid()}";

        public abstract Type ExtensionType { get; }

        // Anything job extension want to do before building the steps list.
        public abstract void InitializeJobExtension(IExecutionContext context, IList<Pipelines.JobStep> steps, Pipelines.WorkspaceOptions workspace);

        // download all required tasks.
        // make sure all task's condition inputs are valid.
        // build up three list of steps for jobrunner. (pre-job, job, post-job)
        public async Task<List<IStep>> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message)
        {
            Trace.Entering();
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNull(message, nameof(message));

            // create a new timeline record node for 'Initialize job'
            IExecutionContext context = jobContext.CreateChild(Guid.NewGuid(), StringUtil.Loc("InitializeJob"), $"{nameof(JobExtension)}_Init");

            List<IStep> preJobSteps = new List<IStep>();
            List<IStep> jobSteps = new List<IStep>();
            List<IStep> postJobSteps = new List<IStep>();
            using (var register = jobContext.CancellationToken.Register(() => { context.CancelToken(); }))
            {
                try
                {
                    context.Start();
                    context.Section(StringUtil.Loc("StepStarting", StringUtil.Loc("InitializeJob")));

                    // Set agent version variable.
                    // context.Variables.Set(Constants.Variables.Agent.Version, BuildConstants.AgentPackage.Version);
                    context.Output(StringUtil.Loc("AgentVersion", BuildConstants.AgentPackage.Version));

                    // Print proxy setting information for better diagnostic experience
                    var agentWebProxy = HostContext.GetService<IVstsAgentWebProxy>();
                    if (!string.IsNullOrEmpty(agentWebProxy.ProxyAddress))
                    {
                        context.Output(StringUtil.Loc("AgentRunningBehindProxy", agentWebProxy.ProxyAddress));
                    }

                    // Give job extension a chance to initialize
                    Trace.Info($"Run initial step from extension {this.GetType().Name}.");
                    InitializeJobExtension(context, message.Steps, message.Workspace);

                    // Download tasks if not already in the cache
                    Trace.Info("Downloading task definitions.");
                    var taskManager = HostContext.GetService<ITaskManager>();
                    await taskManager.DownloadAsync(context, message.Steps);

                    // Parse all Task conditions.
                    Trace.Info("Parsing all task's condition inputs.");
                    var expression = HostContext.GetService<IExpressionManager>();
                    Dictionary<Guid, IExpressionNode> taskConditionMap = new Dictionary<Guid, IExpressionNode>();
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
                            if (step.Type == Pipelines.StepType.Task)
                            {
                                condition = expression.Parse(context, step.Condition, legacy: true);
                            }
                            else if (step.Type == Pipelines.StepType.Action)
                            {
                                condition = expression.Parse(context, step.Condition, legacy: false);
                            }
                            else
                            {
                                throw new NotSupportedException(step.Type.ToString());
                            }
                        }

                        taskConditionMap[step.Id] = condition;
                    }

                    // build up 3 lists of steps, pre-job, job, post-job
                    Stack<IStep> postJobStepsBuilder = new Stack<IStep>();
                    Dictionary<Guid, Variables> taskVariablesMapping = new Dictionary<Guid, Variables>();

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
                                                                          displayName: StringUtil.Loc("InitializeContainer"),
                                                                          data: (object)containers));
                        postJobStepsBuilder.Push(new JobExtensionRunner(runAsync: containerProvider.StopContainersAsync,
                                                                        condition: ExpressionManager.Always,
                                                                        displayName: StringUtil.Loc("StopContainer"),
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
                            actionRunner.Condition = taskConditionMap[action.Id];
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
                            extensionStep.ExecutionContext = jobContext.CreateChild(stepId, extensionStep.DisplayName, stepId.ToString("N"));
                        }
                    }

                    // create task execution context for all job steps from task
                    foreach (var step in jobSteps)
                    {
                        if (step is IActionRunner)
                        {
                            IActionRunner actionStep = step as IActionRunner;
                            ArgUtil.NotNull(actionStep, step.DisplayName);
                            actionStep.ExecutionContext = jobContext.CreateChild(actionStep.Action.Id, actionStep.DisplayName, actionStep.Action.Name, outputForward: true);
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
                            extensionStep.ExecutionContext = jobContext.CreateChild(stepId, extensionStep.DisplayName, stepId.ToString("N"));
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
                        // Set the VSTS_PROCESS_LOOKUP_ID env variable.
                        context.SetRunnerContext(Constants.ProcessLookupId, _processLookupId);
                        context.Output("Start tracking orphan processes.");

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
                    context.Section(StringUtil.Loc("StepFinishing", StringUtil.Loc("InitializeJob")));
                    context.Complete();
                }
            }
        }

        public async Task FinalizeJob(IExecutionContext jobContext)
        {
            Trace.Entering();
            ArgUtil.NotNull(jobContext, nameof(jobContext));

            // create a new timeline record node for 'Finalize job'
            IExecutionContext context = jobContext.CreateChild(Guid.NewGuid(), StringUtil.Loc("FinalizeJob"), $"{nameof(JobExtension)}_Final");
            using (var register = jobContext.CancellationToken.Register(() => { context.CancelToken(); }))
            {
                try
                {
                    context.Start();
                    context.Section(StringUtil.Loc("StepStarting", StringUtil.Loc("FinalizeJob")));

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

                    if (_processCleanup)
                    {
                        context.Output("Start cleaning up orphan processes.");

                        // Only check environment variable for any process that doesn't run before we invoke our process.
                        Dictionary<int, Process> currentProcesses = SnapshotProcesses();
                        foreach (var proc in currentProcesses)
                        {
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
                                    lookupId = proc.Value.GetEnvironmentVariable(HostContext, Constants.ProcessLookupId);
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
                    context.Section(StringUtil.Loc("StepFinishing", StringUtil.Loc("FinalizeJob")));
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