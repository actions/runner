using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
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

            ActionExecutionData handlerData = definition.Data?.Execution;
            ArgUtil.NotNull(handlerData, nameof(handlerData));

            IStepHost stepHost = HostContext.CreateService<IDefaultStepHost>();

            // Makes directory for event_path data
            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);
            var workflowDirectory = Path.Combine(tempDirectory, "_github_workflow");
            Directory.CreateDirectory(workflowDirectory);

            var gitHubEvent = ExecutionContext.GetGitHubContext("event");

            // adds the GitHub event path/file if the event exists
            if (gitHubEvent != null)
            {
                var workflowFile = Path.Combine(workflowDirectory, "event.json");
                Trace.Info($"Write event payload to {workflowFile}");
                File.WriteAllText(workflowFile, gitHubEvent, new UTF8Encoding(false));
                ExecutionContext.SetGitHubContext("event_path", workflowFile);
            }

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
            ExecutionContext.Debug("Loading inputs");
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
                    string key = input.Key.AssertString("action input name").Value;
                    string value = input.Value.AssertString("action input default value").Value;
                    if (!inputs.ContainsKey(key))
                    {
                        inputs[key] = value;
                    }
                }
            }

            // Load the task environment.
            ExecutionContext.Debug("Loading env");
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
                            actionDirectory: definition.Directory);

            // Print out action details
            handler.PrintActionDetails();

            // Run the task.
            await handler.RunAsync();
        }
    }
}
