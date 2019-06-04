using System;
using System.Linq;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker.Handlers;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionRunner))]
    public interface IActionRunner : IStep, IRunnerService
    {
        Pipelines.ActionStep Action { get; set; }
    }

    public sealed class ActionRunner : RunnerService, IActionRunner
    {
        public IExpressionNode Condition { get; set; }

        public bool ContinueOnError => Action?.ContinueOnError ?? default(bool);

        public string DisplayName => Action?.DisplayName;

        public bool Enabled => Action?.Enabled ?? default(bool);

        public IExecutionContext ExecutionContext { get; set; }

        public Pipelines.ActionStep Action { get; set; }

        public TimeSpan? Timeout => (Action?.TimeoutInMinutes ?? 0) > 0 ? (TimeSpan?)TimeSpan.FromMinutes(Action.TimeoutInMinutes) : null;

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Action, nameof(Action));
            var taskManager = HostContext.GetService<IActionManager>();
            var handlerFactory = HostContext.GetService<IHandlerFactory>();

            // Load the task definition and choose the handler.
            Definition definition = taskManager.LoadAction(ExecutionContext, Action);
            ArgUtil.NotNull(definition, nameof(definition));

            // Print out action metadata
            PrintActionMetaData(definition);

            HandlerData handlerData = definition.Data?.Execution?.All?.Single();
            ArgUtil.NotNull(handlerData, nameof(handlerData));

            IStepHost stepHost = HostContext.CreateService<IDefaultStepHost>();

            // Setup container stephost for running inside the container.
            if (ExecutionContext.Container != null)
            {
                // Make sure required container is already created.
                ArgUtil.NotNullOrEmpty(ExecutionContext.Container.ContainerId, nameof(ExecutionContext.Container.ContainerId));
                var containerStepHost = HostContext.CreateService<IContainerStepHost>();
                containerStepHost.Container = ExecutionContext.Container;
                stepHost = containerStepHost;
                throw new NotSupportedException("Call Ting to fix this.");
            }

            // Load the inputs.
            ExecutionContext.Output($"{WellKnownTags.Debug}Loading inputs");
            var templateTrace = ExecutionContext.ToTemplateTraceWriter();
            var schema = new PipelineTemplateSchemaFactory().CreateSchema();
            var templateEvaluator = new PipelineTemplateEvaluator(templateTrace, schema);
            var inputs = templateEvaluator.EvaluateStepInputs(Action.Inputs, ExecutionContext.ExpressionValues);

            // Merge the default inputs from the definition
            if (definition.Data?.Inputs != null)
            {
                var defaultInputsTemplateToken = new MappingToken(null, null, null);
                foreach (var input in (definition.Data?.Inputs))
                {
                    string key = input.Name?.Trim();
                    if (!string.IsNullOrEmpty(key) && !inputs.ContainsKey(key))
                    {
                        var defaultValue = input.DefaultValue?.Trim() ?? string.Empty;
                        defaultInputsTemplateToken.Add(new LiteralToken(null, null, null, key), new LiteralToken(null, null, null, defaultValue));
                    }
                }

                var defaultInputs = templateEvaluator.EvaluateStepInputs(defaultInputsTemplateToken, ExecutionContext.ExpressionValues);
                if (defaultInputs.Count > 0)
                {
                    foreach (var defaultInput in defaultInputs)
                    {
                        if (!inputs.ContainsKey(defaultInput.Key))
                        {
                            inputs[defaultInput.Key] = defaultInput.Value;
                        }
                    }
                }
            }

            // Load the task environment.
            ExecutionContext.Output($"{WellKnownTags.Debug}Loading env");
            var environment = templateEvaluator.EvaluateStepEnvironment(Action.Environment, ExecutionContext.ExpressionValues, VarUtil.EnvironmentVariableKeyComparer);

            // Apply environment set using ##[set-env]
            foreach (var env in ExecutionContext.EnvironmentVariables)
            {
                environment[env.Key] = env.Value ?? string.Empty;
            }

            // Create the handler.
            IHandler handler = handlerFactory.Create(
                            ExecutionContext,
                            Action.Reference,
                            stepHost,
                            handlerData,
                            inputs,
                            environment,
                            ExecutionContext.Variables,
                            taskDirectory: definition.Directory);

            // Run the task.
            await handler.RunAsync();
        }

        private void PrintActionMetaData(Definition actionDefinition)
        {
            ArgUtil.NotNull(Action, nameof(Action));
            ArgUtil.NotNull(Action.Reference, nameof(Action.Reference));
            ArgUtil.NotNull(actionDefinition.Data, nameof(actionDefinition.Data));

            ExecutionContext.Output("==============================================================================");
            ExecutionContext.Output($"Action             : {actionDefinition.Data.FriendlyName}");
            ExecutionContext.Output($"Description        : {actionDefinition.Data.Description}");

            if (Action.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry)
            {
                var registryAction = Action.Reference as Pipelines.ContainerRegistryReference;
                ExecutionContext.Output($"Action image       : {registryAction.Image}");
            }
            else if (Action.Reference.Type == Pipelines.ActionSourceType.Repository)
            {
                var repoAction = Action.Reference as Pipelines.RepositoryPathReference;
                if (string.Equals(repoAction.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
                {
                    ExecutionContext.Output($"Action repository  : {Pipelines.PipelineConstants.SelfAlias}");
                }
                else
                {
                    ExecutionContext.Output($"Action repository  : {repoAction.Name}@{repoAction.Ref}");
                }
            }

            ExecutionContext.Output($"Author             : {actionDefinition.Data.Author}");
            ExecutionContext.Output("==============================================================================");
        }
    }
}
