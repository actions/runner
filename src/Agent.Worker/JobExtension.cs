using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IJobExtension : IExtension
    {
        HostTypes HostType { get; }
        Task<JobInitializeResult> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message);
        string GetRootedPath(IExecutionContext context, string path);
        void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath);
    }

    public sealed class JobInitializeResult
    {
        private List<IStep> _preJobSteps = new List<IStep>();
        private List<IStep> _jobSteps = new List<IStep>();
        private List<IStep> _postJobSteps = new List<IStep>();

        public List<IStep> PreJobSteps => _preJobSteps;
        public List<IStep> JobSteps => _jobSteps;
        public List<IStep> PostJobStep => _postJobSteps;
    }

    public abstract class JobExtension : AgentService, IJobExtension
    {
        public abstract HostTypes HostType { get; }

        public abstract Type ExtensionType { get; }

        // Anything job extension want to do before building the steps list.
        public abstract void InitializeJobExtension(IExecutionContext context);

        // Anything job extension want to add to pre-job steps list. This will be deprecated when GetSource move to a task.
        public abstract IStep GetExtensionPreJobStep(IExecutionContext jobContext);

        // Anything job extension want to add to post-job steps list. This will be deprecated when GetSource move to a task.
        public abstract IStep GetExtensionPostJobStep(IExecutionContext jobContext);

        public abstract string GetRootedPath(IExecutionContext context, string path);

        public abstract void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath);

        // download all required tasks.
        // make sure all task's condition inputs are valid.
        // build up three list of steps for jobrunner. (pre-job, job, post-job)
        public async Task<JobInitializeResult> InitializeJob(IExecutionContext jobContext, Pipelines.AgentJobRequestMessage message)
        {
            Trace.Entering();
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNull(message, nameof(message));

            // create a new timeline record node for 'Initialize job'
            IExecutionContext context = jobContext.CreateChild(Guid.NewGuid(), StringUtil.Loc("InitializeJob"), nameof(JobExtension));

            JobInitializeResult initResult = new JobInitializeResult();
            using (var register = jobContext.CancellationToken.Register(() => { context.CancelToken(); }))
            {
                try
                {
                    context.Start();
                    context.Section(StringUtil.Loc("StepStarting", StringUtil.Loc("InitializeJob")));

                    // Give job extension a chance to initialize
                    Trace.Info($"Run initial step from extension {this.GetType().Name}.");
                    InitializeJobExtension(context);

                    // Download tasks if not already in the cache
                    Trace.Info("Downloading task definitions.");
                    var taskManager = HostContext.GetService<ITaskManager>();
                    await taskManager.DownloadAsync(context, message.Steps);

                    // Parse all Task conditions.
                    Trace.Info("Parsing all task's condition inputs.");
                    var expression = HostContext.GetService<IExpressionManager>();
                    Dictionary<Guid, INode> taskConditionMap = new Dictionary<Guid, INode>();
                    foreach (var task in message.Steps.OfType<Pipelines.TaskStep>())
                    {
                        INode condition;
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
                        initResult.PreJobSteps.Add(prepareStep);
                    }
#endif

                    // build up 3 lists of steps, pre-job, job, post-job
                    Stack<IStep> postJobStepsBuilder = new Stack<IStep>();
                    Dictionary<Guid, Variables> taskVariablesMapping = new Dictionary<Guid, Variables>();

                    if (context.Container != null)
                    {
                        var containerProvider = HostContext.GetService<IContainerOperationProvider>();
                        initResult.PreJobSteps.Add(new JobExtensionRunner(runAsync: containerProvider.StartContainerAsync,
                                                                          condition: ExpressionManager.Succeeded,
                                                                          displayName: StringUtil.Loc("InitializeContainer"),
                                                                          data: (object)jobContext.Container));
                        postJobStepsBuilder.Push(new JobExtensionRunner(runAsync: containerProvider.StopContainerAsync,
                                                                        condition: ExpressionManager.Always,
                                                                        displayName: StringUtil.Loc("StopContainer"),
                                                                        data: (object)jobContext.Container));
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
                            initResult.PreJobSteps.Add(taskRunner);
                        }

                        // Add execution steps from Tasks
                        if (taskDefinition.Data?.Execution != null)
                        {
                            Trace.Verbose($"Adding {task.DisplayName}.");
                            var taskRunner = HostContext.CreateService<ITaskRunner>();
                            taskRunner.Task = task;
                            taskRunner.Stage = JobRunStage.Main;
                            taskRunner.Condition = taskConditionMap[task.Id];
                            initResult.JobSteps.Add(taskRunner);
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
                        initResult.PreJobSteps.Add(extensionPreJobStep);
                    }

                    // Add post-job step from Extension
                    Trace.Info("Adding post-job step from extension.");
                    var extensionPostJobStep = GetExtensionPostJobStep(jobContext);
                    if (extensionPostJobStep != null)
                    {
                        postJobStepsBuilder.Push(extensionPostJobStep);
                    }

                    // create execution context for all pre-job steps
                    foreach (var step in initResult.PreJobSteps)
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
                            taskStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), StringUtil.Loc("PreJob", taskStep.DisplayName), taskStep.Task.Name, taskVariablesMapping[taskStep.Task.Id]);
                        }
                    }

                    // create task execution context for all job steps from task
                    foreach (var step in initResult.JobSteps)
                    {
                        ITaskRunner taskStep = step as ITaskRunner;
                        ArgUtil.NotNull(taskStep, step.DisplayName);
                        taskStep.ExecutionContext = jobContext.CreateChild(taskStep.Task.Id, taskStep.DisplayName, taskStep.Task.Name, taskVariablesMapping[taskStep.Task.Id]);
                    }

                    // Add post-job steps from Tasks
                    Trace.Info("Adding post-job steps from tasks.");
                    while (postJobStepsBuilder.Count > 0)
                    {
                        initResult.PostJobStep.Add(postJobStepsBuilder.Pop());
                    }

                    // create task execution context for all post-job steps from task
                    foreach (var step in initResult.PostJobStep)
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
                            taskStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), StringUtil.Loc("PostJob", taskStep.DisplayName), taskStep.Task.Name, taskVariablesMapping[taskStep.Task.Id]);
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
                        initResult.PostJobStep.Add(finallyStep);
                    }
#endif
                    return initResult;
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
    }
}