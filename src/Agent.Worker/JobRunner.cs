using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(JobRunner))]
    public interface IJobRunner : IAgentService
    {
        Task<TaskResult> RunAsync(JobRequestMessage message);
    }

    public sealed class JobRunner : AgentService, IJobRunner
    {
        public async Task<TaskResult> RunAsync(JobRequestMessage message)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            ArgUtil.NotNull(message.Environment.Variables, nameof(message.Environment.Variables));
            ArgUtil.NotNull(message.Tasks, nameof(message.Tasks));
            Trace.Info("Job ID {0}", message.JobId);

            // Setup the job server and job server queue.
            var jobServer = HostContext.GetService<IJobServer>();
            await jobServer.ConnectAsync(ApiUtil.GetVssConnection(message));
            var jobServerQueue = HostContext.GetService<IJobServerQueue>();
            jobServerQueue.Start(message);

            IExecutionContext jobContext = null;
            try
            {
                // Create the job execution context.
                jobContext = HostContext.CreateService<IExecutionContext>();
                jobContext.InitializeJob(message);
                Trace.Info("Starting the job execution context.");
                jobContext.Start();

                // Get the job extensions.
                Trace.Info("Getting job extensions.");
                string hostType = jobContext.Variables.System_HostType;
                var extensionManager = HostContext.GetService<IExtensionManager>();
                IJobExtension[] extensions =
                    (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                    .Where(x => string.Equals(x.HostType, hostType, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // Add the prepare steps.
                Trace.Info("Adding job prepare extensions.");
                List<IStep> steps = new List<IStep>();
                foreach (IJobExtension extension in extensions)
                {
                    if (extension.PrepareStep != null)
                    {
                        Trace.Verbose($"Adding {extension.GetType().Name}.{nameof(extension.PrepareStep)}.");
                        extension.PrepareStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), extension.PrepareStep.DisplayName);
                        steps.Add(extension.PrepareStep);
                    }
                }

                // Add the task steps.
                Trace.Info("Adding tasks.");
                foreach (TaskInstance taskInstance in message.Tasks)
                {
                    Trace.Verbose($"Adding {taskInstance.DisplayName}.");
                    var taskRunner = HostContext.CreateService<ITaskRunner>();
                    taskRunner.ExecutionContext = jobContext.CreateChild(taskInstance.InstanceId, taskInstance.DisplayName);
                    taskRunner.TaskInstance = taskInstance;
                    steps.Add(taskRunner);
                }

                // Add the finally steps.
                Trace.Info("Adding job finally extensions.");
                foreach (IJobExtension extension in extensions)
                {
                    if (extension.FinallyStep != null)
                    {
                        Trace.Verbose($"Adding {extension.GetType().Name}.{nameof(extension.FinallyStep)}.");
                        extension.FinallyStep.ExecutionContext = jobContext.CreateChild(Guid.NewGuid(), extension.FinallyStep.DisplayName);
                        steps.Add(extension.FinallyStep);
                    }
                }

                // Download tasks if not already in the cache
                Trace.Info("Downloading task definitions.");
                var taskManager = HostContext.GetService<ITaskManager>();
                try
                {
                    await taskManager.DownloadAsync(jobContext, message.Tasks);
                }
                catch (Exception ex)
                {
                    // Log the error and fail the job.
                    Trace.Error($"Caught exception from {nameof(TaskManager)}: {ex}");
                    jobContext.Error(ex);
                    jobContext.Result = TaskResult.Failed;
                    return jobContext.Result.Value;
                }

                // TODO: Recursive expand variables before running the steps. Detect cycles and warn if a cyclical reference is encountered. Depth limit of 50. Use a stack, not recursive function.

                // Run the steps.
                var stepsRunner = HostContext.GetService<IStepsRunner>();
                try
                {
                    await stepsRunner.RunAsync(jobContext, steps);
                }
                catch (Exception ex)
                {
                    // Log the error and fail the job.
                    Trace.Error($"Caught exception from {nameof(StepsRunner)}: {ex}");
                    jobContext.Error(ex);
                    jobContext.Result = TaskResult.Failed;
                    return jobContext.Result.Value;
                }

                Trace.Info($"Job result: {jobContext.Result}");
                return jobContext.Result ?? TaskResult.Succeeded;
            }
            // Only handle the exception if we can set the job result. Otherwise let it bubble.
            catch (Exception ex) when (jobContext != null)
            {
                // Log the error and fail the job.
                Trace.Error($"Caught exception: {ex}");
                jobContext.Error(ex);
                jobContext.Result = TaskResult.Failed;
                return jobContext.Result.Value;
            }
            finally
            {
                // Complete the job.
                if (jobContext != null)
                {
                    Trace.Info("Completing the job execution context.");
                    jobContext.Complete();
                }

                // Drain the job server queue.
                if (jobServerQueue != null)
                {
                    try
                    {
                        Trace.Info("Shutting down the job server queue.");
                        await jobServerQueue.ShutdownAsync();
                    }
                    catch (Exception ex)
                    {
                        Trace.Error($"Caught exception from {nameof(JobServerQueue)}.{nameof(jobServerQueue.ShutdownAsync)}: {ex}");
                    }
                }
            }
        }
    }
}
