using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IJobExtension : IExtension
    {
        HostTypes HostType { get; }
        Task<JobInitializeResult> InitializeJob(IExecutionContext jobContext, AgentJobRequestMessage message);
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

        // Anything job extension want to add to pre-job steps list.
        public abstract IStep GetExtensionPreJobStep(IExecutionContext jobContext);

        // Anything job extension want to add to post-job steps list.
        public abstract IStep GetExtensionPostJobStep(IExecutionContext jobContext);

        public abstract string GetRootedPath(IExecutionContext context, string path);

        public abstract void ConvertLocalPath(IExecutionContext context, string localPath, out string repoName, out string sourcePath);

        // download all required tasks.
        // make sure all task's condition inputs are valid.
        // build up three list of steps for jobrunner. (pre-job, job, post-job)
        public async Task<JobInitializeResult> InitializeJob(IExecutionContext jobContext, AgentJobRequestMessage message)
        {
            Trace.Entering();
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNull(message, nameof(message));

            // create a new timeline record node for 'Initialize job'
            IExecutionContext context = jobContext.CreateChild(Guid.NewGuid(), StringUtil.Loc("InitializeJob"));

            JobInitializeResult initResult = new JobInitializeResult();
            using (var register = jobContext.CancellationToken.Register(() => { context.CancelToken(); }))
            {
                try
                {
                    context.Start();
                    context.Section(StringUtil.Loc("StepStarting", StringUtil.Loc("InitializeJob")));

                    // Give job extension a chance to initalize
                    Trace.Info($"Run initial step from extension {this.GetType().Name}.");
                    InitializeJobExtension(context);

                    // Download tasks if not already in the cache
                    Trace.Info("Downloading task definitions.");
                    var taskManager = HostContext.GetService<ITaskManager>();
                    await taskManager.DownloadAsync(context, message.Tasks);

                    // Parse all Task conditions.
                    Trace.Info("Parsing all task's condition inputs.");
                    var expression = HostContext.GetService<IExpressionManager>();
                    Dictionary<Guid, INode> taskConditionMap = new Dictionary<Guid, INode>();
                    foreach (var task in message.Tasks)
                    {
                        INode condition;
                        if (!string.IsNullOrEmpty(task.Condition))
                        {
                            context.Debug($"Task '{task.DisplayName}' has following condition: '{task.Condition}'.");
                            condition = expression.Parse(context, task.Condition);
                        }
                        else if (task.AlwaysRun)
                        {
                            condition = ExpressionManager.SucceededOrFailed;
                        }
                        else
                        {
                            condition = ExpressionManager.Succeeded;
                        }

                        taskConditionMap[task.InstanceId] = condition;
                    }

#if OS_WINDOWS
                    // This is for internal testing and is not publicly supported. This will be removed from the agent at a later time.
                    var prepareScript = Environment.GetEnvironmentVariable("VSTS_AGENT_INIT_INTERNAL_TEMP_HACK");
                    if (!string.IsNullOrEmpty(prepareScript))
                    {
                        var prepareStep = new ManagementScriptStep(
                            scriptPath: prepareScript,
                            condition: ExpressionManager.Succeeded,
                            displayName: "Agent Initialization");

                        Trace.Verbose($"Adding agent init script step.");
                        prepareStep.Initialize(HostContext);
                        prepareStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), prepareStep.DisplayName);
                        prepareStep.AccessToken = message.Environment.SystemConnection.Authorization.Parameters["AccessToken"];
                        initResult.PreJobSteps.Add(prepareStep);
                    }
#endif

                    // Add pre-job steps from Tasks
                    Trace.Info("Adding pre-job steps from tasks.");
                    initResult.PreJobSteps.AddRange(taskManager.GetTasksPreJobSteps(jobContext, message.Tasks, taskConditionMap));

                    // Add pre-job step from Extension
                    Trace.Info("Adding pre-job step from extension.");
                    var extensionPreJobStep = GetExtensionPreJobStep(jobContext);
                    if (extensionPreJobStep != null)
                    {
                        initResult.PreJobSteps.Add(extensionPreJobStep);
                    }

                    // Add execution steps from Tasks
                    Trace.Info("Adding tasks.");
                    initResult.JobSteps.AddRange(taskManager.GetTasksMainSteps(jobContext, message.Tasks, taskConditionMap));

                    // Add post-job steps from Tasks
                    Trace.Info("Adding post-job steps from tasks.");
                    initResult.PostJobStep.AddRange(taskManager.GetTasksPostJobSteps(jobContext, message.Tasks, taskConditionMap));

                    // Add post-job step from Extension
                    Trace.Info("Adding post-job step from extension.");
                    var extensionPostJobStep = GetExtensionPostJobStep(jobContext);
                    if (extensionPostJobStep != null)
                    {
                        initResult.PostJobStep.Add(extensionPostJobStep);
                    }

#if OS_WINDOWS
                    // Add script post steps.
                    // This is for internal testing and is not publicly supported. This will be removed from the agent at a later time.
                    var finallyScript = Environment.GetEnvironmentVariable("VSTS_AGENT_CLEANUP_INTERNAL_TEMP_HACK");
                    if (!string.IsNullOrEmpty(finallyScript))
                    {
                        var finallyStep = new ManagementScriptStep(
                            scriptPath: finallyScript,
                            condition: ExpressionManager.Always,
                            displayName: "Agent Cleanup");

                        Trace.Verbose($"Adding agent cleanup script step.");
                        finallyStep.Initialize(HostContext);
                        finallyStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), finallyStep.DisplayName);
                        finallyStep.AccessToken = message.Environment.SystemConnection.Authorization.Parameters["AccessToken"];
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