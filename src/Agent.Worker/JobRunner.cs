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
        Task<int> RunAsync(JobRequestMessage message);
    }

    public sealed class JobRunner : AgentService, IJobRunner
    {
        public async Task<int> RunAsync(JobRequestMessage message)
        {
            Trace.Entering();

            // Validate parameters.
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            ArgUtil.NotNull(message.Environment.Variables, nameof(message.Environment.Variables));
            ArgUtil.NotNull(message.Tasks, nameof(message.Tasks));

            Trace.Info("Job ID {0}", message.JobId);

            var stepsRunner = HostContext.GetService<IStepsRunner>();
            var extensionManager = HostContext.GetService<IExtensionManager>();

            var jobServer = HostContext.GetService<IJobServer>();
            await jobServer.ConnectAsync(ApiUtil.GetVssConnection(message));

            var jobServerQueue = HostContext.GetService<IJobServerQueue>();
            jobServerQueue.Start(message);
            try
            {
                // Create the job execution context.
                var jobExecutionContext = HostContext.CreateService<IExecutionContext>();
                jobExecutionContext.InitializeJob(message);
                jobExecutionContext.Start();

                // Get the job extensions.
                string hostType = jobExecutionContext.Variables.System_HostType;
                IJobExtension[] extensions =
                    (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                    .Where(x => string.Equals(x.HostType, hostType, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // Add the prepare steps.
                List<IStep> steps = new List<IStep>();
                foreach (IJobExtension extension in extensions)
                {
                    if (extension.PrepareStep != null)
                    {
                        Trace.Verbose($"Adding {extension.GetType().Name}.{nameof(extension.PrepareStep)}.");
                        extension.PrepareStep.ExecutionContext = jobExecutionContext.CreateChild(Guid.NewGuid(), extension.PrepareStep.DisplayName);
                        steps.Add(extension.PrepareStep);
                    }
                }

                // Add the task steps.
                foreach (TaskInstance taskInstance in message.Tasks)
                {
                    Trace.Verbose($"Adding {taskInstance.DisplayName}.");
                    var taskRunner = HostContext.CreateService<ITaskRunner>();
                    taskRunner.ExecutionContext = jobExecutionContext.CreateChild(taskInstance.InstanceId, taskInstance.DisplayName);
                    taskRunner.TaskInstance = taskInstance;
                    steps.Add(taskRunner);
                }

                // Add the finally steps.
                foreach (IJobExtension extension in extensions)
                {
                    if (extension.FinallyStep != null)
                    {
                        Trace.Verbose($"Adding {extension.GetType().Name}.{nameof(extension.FinallyStep)}.");
                        extension.FinallyStep.ExecutionContext = jobExecutionContext.CreateChild(Guid.NewGuid(), extension.FinallyStep.DisplayName);
                        steps.Add(extension.FinallyStep);
                    }
                }

                // Download tasks if not already in the cache
                var taskManager = HostContext.GetService<ITaskManager>();
                await taskManager.EnsureTasksExist(message.Tasks);

                // Run the steps.
                jobExecutionContext.Result = await stepsRunner.RunAsync(jobExecutionContext, steps);

                jobExecutionContext.Complete();
            }
            finally
            {
                try
                {
                    await jobServerQueue.ShutdownAsync();
                }
                catch (Exception ex)
                {
                    Trace.Error($"Error during {nameof(jobServerQueue.ShutdownAsync)}: {ex}");
                }
            }

            return 0;
        }
    }
}
