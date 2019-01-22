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
        HostTypes HostType { get; }
        Task<List<IStep>> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message);
        Task FinalizeJob(IExecutionContext jobContext);
        string GetRootedPath(IExecutionContext context, string path);
        void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath);
    }

    public abstract class JobExtension : AgentService, IJobExtension
    {
        private readonly HashSet<string> _existingProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _processCleanup;
        private string _processLookupId = $"vsts_{Guid.NewGuid()}";

        public abstract HostTypes HostType { get; }

        public abstract Type ExtensionType { get; }

        // Anything job extension want to do before building the steps list.
        public abstract void InitializeJobExtension(IExecutionContext context, IList<Pipelines.JobStep> steps, Pipelines.WorkspaceOptions workspace);

        // Anything job extension want to add to pre-job steps list. This will be deprecated when GetSource move to a task.
        public abstract IStep GetExtensionPreJobStep(IExecutionContext jobContext);

        // Anything job extension want to add to post-job steps list. This will be deprecated when GetSource move to a task.
        public abstract IStep GetExtensionPostJobStep(IExecutionContext jobContext);

        public abstract string GetRootedPath(IExecutionContext context, string path);

        public abstract void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath);

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
                    context.Variables.Set(Constants.Variables.Agent.Version, Constants.Agent.Version);
                    context.Output(StringUtil.Loc("AgentVersion", Constants.Agent.Version));

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
                    foreach (var task in message.Steps.OfType<Pipelines.TaskStep>())
                    {
                        IExpressionNode condition;
                        if (!string.IsNullOrEmpty(task.Condition))
                        {
                            context.Debug($"Task '{task.DisplayName}' has following condition: '{task.Condition}'.");
                            condition = expression.Parse(context, task.Condition);
                        }
                        else
                        {
                            condition = ExpressionManager.Succeeded;
                        }

                        taskConditionMap[task.Id] = condition;
                    }

#if OS_WINDOWS
                    // This is for internal testing and is not publicly supported. This will be removed from the agent at a later time.
                    var prepareScript = Environment.GetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK");
                    ServiceEndpoint systemConnection = context.Endpoints.Single(x => string.Equals(x.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(prepareScript) && context.Container == null)
                    {
                        var prepareStep = new ManagementScriptStep(
                            scriptPath: prepareScript,
                            condition: ExpressionManager.Succeeded,
                            displayName: "Agent Initialization");

                        Trace.Verbose($"Adding agent init script step.");
                        prepareStep.Initialize(HostContext);
                        prepareStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), prepareStep.DisplayName, nameof(ManagementScriptStep));
                        prepareStep.AccessToken = systemConnection.Authorization.Parameters["AccessToken"];
                        prepareStep.Condition = ExpressionManager.Succeeded;
                        preJobSteps.Add(prepareStep);
                    }
#endif

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

                    foreach (var task in message.Steps.OfType<Pipelines.TaskStep>())
                    {
                        var taskDefinition = taskManager.Load(task);

                        List<string> warnings;
                        taskVariablesMapping[task.Id] = new Variables(HostContext, new Dictionary<string, VariableValue>(), out warnings);

                        // Add pre-job steps from Tasks
                        if (taskDefinition.Data?.PreJobExecution != null)
                        {
                            Trace.Info($"Adding Pre-Job {task.DisplayName}.");
                            var taskRunner = HostContext.CreateService<ITaskRunner>();
                            taskRunner.Task = task;
                            taskRunner.Stage = JobRunStage.PreJob;
                            taskRunner.Condition = taskConditionMap[task.Id];
                            preJobSteps.Add(taskRunner);
                        }

                        // Add execution steps from Tasks
                        if (taskDefinition.Data?.Execution != null)
                        {
                            Trace.Verbose($"Adding {task.DisplayName}.");
                            var taskRunner = HostContext.CreateService<ITaskRunner>();
                            taskRunner.Task = task;
                            taskRunner.Stage = JobRunStage.Main;
                            taskRunner.Condition = taskConditionMap[task.Id];
                            jobSteps.Add(taskRunner);
                        }

                        // Add post-job steps from Tasks
                        if (taskDefinition.Data?.PostJobExecution != null)
                        {
                            Trace.Verbose($"Adding Post-Job {task.DisplayName}.");
                            var taskRunner = HostContext.CreateService<ITaskRunner>();
                            taskRunner.Task = task;
                            taskRunner.Stage = JobRunStage.PostJob;
                            taskRunner.Condition = ExpressionManager.Always;
                            postJobStepsBuilder.Push(taskRunner);
                        }
                    }

                    // Add pre-job step from Extension
                    Trace.Info("Adding pre-job step from extension.");
                    var extensionPreJobStep = GetExtensionPreJobStep(jobContext);
                    if (extensionPreJobStep != null)
                    {
                        preJobSteps.Add(extensionPreJobStep);
                    }

                    // Add post-job step from Extension
                    Trace.Info("Adding post-job step from extension.");
                    var extensionPostJobStep = GetExtensionPostJobStep(jobContext);
                    if (extensionPostJobStep != null)
                    {
                        postJobStepsBuilder.Push(extensionPostJobStep);
                    }

                    // create execution context for all pre-job steps
                    foreach (var step in preJobSteps)
                    {
#if OS_WINDOWS
                        if (step is ManagementScriptStep)
                        {
                            continue;
                        }
#endif
                        if (step is JobExtensionRunner)
                        {
                            JobExtensionRunner extensionStep = step as JobExtensionRunner;
                            ArgUtil.NotNull(extensionStep, extensionStep.DisplayName);
                            Guid stepId = Guid.NewGuid();
                            extensionStep.ExecutionContext = jobContext.CreateChild(stepId, extensionStep.DisplayName, stepId.ToString("N"));
                        }
                        else if (step is ITaskRunner)
                        {
                            ITaskRunner taskStep = step as ITaskRunner;
                            ArgUtil.NotNull(taskStep, step.DisplayName);
                            taskStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), StringUtil.Loc("PreJob", taskStep.DisplayName), taskStep.Task.Name, taskVariablesMapping[taskStep.Task.Id], outputForward: true);
                        }
                    }

                    // create task execution context for all job steps from task
                    foreach (var step in jobSteps)
                    {
                        ITaskRunner taskStep = step as ITaskRunner;
                        ArgUtil.NotNull(taskStep, step.DisplayName);
                        taskStep.ExecutionContext = jobContext.CreateChild(taskStep.Task.Id, taskStep.DisplayName, taskStep.Task.Name, taskVariablesMapping[taskStep.Task.Id], outputForward: true);
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
                        else if (step is ITaskRunner)
                        {
                            ITaskRunner taskStep = step as ITaskRunner;
                            ArgUtil.NotNull(taskStep, step.DisplayName);
                            taskStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), StringUtil.Loc("PostJob", taskStep.DisplayName), taskStep.Task.Name, taskVariablesMapping[taskStep.Task.Id], outputForward: true);
                        }
                    }

#if OS_WINDOWS
                    // Add script post steps.
                    // This is for internal testing and is not publicly supported. This will be removed from the agent at a later time.
                    var finallyScript = Environment.GetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK");
                    if (!string.IsNullOrEmpty(finallyScript) && context.Container == null)
                    {
                        var finallyStep = new ManagementScriptStep(
                            scriptPath: finallyScript,
                            condition: ExpressionManager.Always,
                            displayName: "Agent Cleanup");

                        Trace.Verbose($"Adding agent cleanup script step.");
                        finallyStep.Initialize(HostContext);
                        finallyStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), finallyStep.DisplayName, nameof(ManagementScriptStep));
                        finallyStep.Condition = ExpressionManager.Always;
                        finallyStep.AccessToken = systemConnection.Authorization.Parameters["AccessToken"];
                        postJobSteps.Add(finallyStep);
                    }
#endif
                    List<IStep> steps = new List<IStep>();
                    steps.AddRange(preJobSteps);
                    steps.AddRange(jobSteps);
                    steps.AddRange(postJobSteps);

                    // Start agent log plugin host process
                    var logPlugin = HostContext.GetService<IAgentLogPlugin>();
                    await logPlugin.StartAsync(context, steps, jobContext.CancellationToken);

                    // Prepare for orphan process cleanup
                    _processCleanup = jobContext.Variables.GetBoolean("process.clean") ?? true;
                    if (_processCleanup)
                    {
                        // Set the VSTS_PROCESS_LOOKUP_ID env variable.
                        context.SetVariable(Constants.ProcessLookupId, _processLookupId, false, false);
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
                    var logPlugin = HostContext.GetService<IAgentLogPlugin>();
                    try
                    {
                        await logPlugin.WaitAsync(context);
                    }
                    catch (Exception ex)
                    {
                        // Log and ignore the error from log plugin finalization.
                        Trace.Error($"Caught exception from log plugin finalization: {ex}");
                        context.Output(ex.Message);
                    }

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