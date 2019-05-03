using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.ObjectTemplating;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.ObjectTemplating;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(ScriptRunner))]
    public interface IScriptRunner : IStep, IAgentService
    {
        Pipelines.ScriptStep Script { get; set; }
    }

    public sealed class ScriptRunner : AgentService, IScriptRunner
    {
        public IExpressionNode Condition { get; set; }

        public bool ContinueOnError => Script?.ContinueOnError ?? default(bool);

        public string DisplayName => Script?.DisplayName;

        public bool Enabled => Script?.Enabled ?? default(bool);

        public IExecutionContext ExecutionContext { get; set; }

        public Pipelines.ScriptStep Script { get; set; }

        public TimeSpan? Timeout => (Script?.TimeoutInMinutes ?? 0) > 0 ? (TimeSpan?)TimeSpan.FromMinutes(Script.TimeoutInMinutes) : null;

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Script, nameof(Script));
            var taskManager = HostContext.GetService<ITaskManager>();
            var handlerFactory = HostContext.GetService<IHandlerFactory>();

            IStepHost stepHost = HostContext.CreateService<IDefaultStepHost>();

            // Setup container stephost for running inside the container.
            if (ExecutionContext.Container != null)
            {
                // Make sure required container is already created.
                ArgUtil.NotNullOrEmpty(ExecutionContext.Container.ContainerId, nameof(ExecutionContext.Container.ContainerId));
                var containerStepHost = HostContext.CreateService<IContainerStepHost>();
                containerStepHost.Container = ExecutionContext.Container;
                stepHost = containerStepHost;
            }

            // Load the inputs.
            ExecutionContext.Output($"{WellKnownTags.Debug}Loading inputs");
            var templateTrace = ExecutionContext.ToTemplateTraceWriter();
            var schema = new PipelineTemplateSchemaFactory().CreateSchema();
            var templateEvaluator = new PipelineTemplateEvaluator(templateTrace, schema);
            var inputs = templateEvaluator.EvaluateStepInputs(Script.Inputs, ExecutionContext.ExpressionValues);

            // Load the task environment.
            ExecutionContext.Output($"{WellKnownTags.Debug}Loading env");
            var environment = templateEvaluator.EvaluateStepEnvironment(Script.Environment, ExecutionContext.ExpressionValues, VarUtil.EnvironmentVariableKeyComparer);

            // Create the handler.
            var handler = HostContext.CreateService<IScriptHandler>();
            handler.ExecutionContext = ExecutionContext;
            handler.StepHost = stepHost;
            handler.Inputs = inputs;
            handler.Environment = environment;
            handler.RuntimeVariables = ExecutionContext.Variables;

            // Run the task.
            await handler.RunAsync();
        }
    }
}
