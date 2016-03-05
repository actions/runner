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
            Trace.Info("Job ID {0}", message.JobId);
            var stepsRunner = HostContext.GetService<IStepsRunner>();
            var extensionManager = HostContext.GetService<IExtensionManager>();
            var jobServer = HostContext.GetService<IJobServer>();
            await jobServer.ConnectAsync(ApiUtil.GetVssConnection(message));

            // Validate parameters.
            if (message == null) { throw new ArgumentNullException(nameof(message)); }
            if (message.Environment == null) { throw new ArgumentNullException(nameof(message.Environment)); }
            if (message.Environment.Variables == null) { throw new ArgumentNullException(nameof(message.Environment.Variables)); }
            if (message.Tasks == null) { throw new ArgumentNullException(nameof(message.Tasks)); }

            // Create the job execution context.
            var jobExecutionContext = HostContext.CreateService<IExecutionContext>();

            // Get the job extensions.
            string hostType = message.Environment.Variables[Constants.Variables.System.HostType];
            IJobExtension[] extensions =
                (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                .Where(x => string.Equals(x.HostType, hostType, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            // Add the prepare steps.
            List<IStep> steps = new List<IStep>();
            foreach (IStep prepareStep in extensions.Select(x => x.PrepareStep).Where(x => x != null))
            {
                prepareStep.ExecutionContext = jobExecutionContext.CreateChild();
                steps.Add(prepareStep);
            }

            // Add the task steps.
            foreach (TaskInstance taskInstance in message.Tasks)
            {
               var taskRunner = HostContext.CreateService<ITaskRunner>();
                taskRunner.ExecutionContext = jobExecutionContext.CreateChild();
                // TODO: taskRunner.TaskInstance = taskInstance;
                steps.Add(taskRunner);
            }

            // Add the finally steps.
            foreach (IStep finallyStep in extensions.Select(x => x.FinallyStep).Where(x => x != null))
            {
                finallyStep.ExecutionContext = jobExecutionContext.CreateChild();
                steps.Add(finallyStep);
            }

            //download tasks if not already in the cache
            var taskManager = HostContext.GetService<ITaskManager>();
            foreach (TaskInstance taskInstance in message.Tasks)
            {
                await taskManager.EnsureTaskExists(taskInstance.Name, 
                    taskInstance.Id, taskInstance.Version);
            }

            // Run the steps.
            await stepsRunner.RunAsync(jobExecutionContext, steps);
            return 0;
        }
    }
}
