using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
            var stepsRunner = HostContext.GetService<IStepsRunner>();
            var extensionManager = HostContext.GetService<IExtensionManager>();

            // Validate parameters.
            ArgUtil.NotNull(message, nameof(message));
            ArgUtil.NotNull(message.Environment, nameof(message.Environment));
            ArgUtil.NotNull(message.Environment.Variables, nameof(message.Environment.Variables));
            ArgUtil.NotNull(message.Tasks, nameof(message.Tasks));
            Trace.Info("Job ID {0}", message.JobId);

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
                taskRunner.TaskInstance = taskInstance;
                steps.Add(taskRunner);
            }

            // Add the finally steps.
            foreach (IStep finallyStep in extensions.Select(x => x.FinallyStep).Where(x => x != null))
            {
                finallyStep.ExecutionContext = jobExecutionContext.CreateChild();
                steps.Add(finallyStep);
            }

            // Run the steps.
            await stepsRunner.RunAsync(jobExecutionContext, steps);
            return 0;
        }
    }
}
