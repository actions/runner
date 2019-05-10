using System;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.ObjectTemplating;
using Microsoft.VisualStudio.Services.Agent.Util;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Worker.Container;
using System.Linq;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class JobExtensionRunner : IStep
    {
        private readonly object _data;
        private readonly Func<IExecutionContext, object, Task> _runAsync;

        public JobExtensionRunner(
            Func<IExecutionContext, object, Task> runAsync,
            IExpressionNode condition,
            string displayName,
            object data)
        {
            _runAsync = runAsync;
            Condition = condition;
            DisplayName = displayName;
            _data = data;
        }

        public IExpressionNode Condition { get; set; }
        public bool ContinueOnError => false;
        public string DisplayName { get; private set; }
        public bool Enabled => true;
        public IExecutionContext ExecutionContext { get; set; }
        public TimeSpan? Timeout => null;

        public async Task RunAsync()
        {
            await _runAsync(ExecutionContext, _data);
        }
    }


    [ServiceLocator(Default = typeof(ActionRunner))]
    public interface IActionRunner : IStep, IAgentService
    {
        Pipelines.ActionStep Action { get; set; }
    }

    public sealed class ActionRunner : AgentService, IActionRunner
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
            ArgUtil.NotNull(ExecutionContext.Variables, nameof(ExecutionContext.Variables));
            ArgUtil.NotNull(Action, nameof(Action));
            var taskManager = HostContext.GetService<ITaskManager>();
            var handlerFactory = HostContext.GetService<IHandlerFactory>();

            // // Set the task id and display name variable.
            // ExecutionContext.Variables.Set(Constants.Variables.Task.DisplayName, DisplayName);
            // ExecutionContext.Variables.Set(WellKnownDistributedTaskVariables.TaskInstanceId, Task.Id.ToString("D"));
            // ExecutionContext.Variables.Set(WellKnownDistributedTaskVariables.TaskDisplayName, DisplayName);
            // ExecutionContext.Variables.Set(WellKnownDistributedTaskVariables.TaskInstanceName, Task.Name);

            // Load the task definition and choose the handler.
            // TODO: Add a try catch here to give a better error message.
            Definition definition = taskManager.LoadAction(ExecutionContext, Action);
            ArgUtil.NotNull(definition, nameof(definition));

            // Print out task metadata
            PrintTaskMetaData(definition);

            // ExecutionData currentExecution = null;
            // switch (definition.Type)
            // {
            //     case ActionType.Container:
            //         await RunContainerActionAsync(executionContext);
            //         break;
            //     case ActionType.NodeScript:
            //         await RunNodeScriptActionAsync();
            //         break;
            //     default:
            //         throw new NotSupportedException(definition.Type.ToString());
            // };

            HandlerData handlerData = definition.Data?.Execution?.All?.Single();
            ArgUtil.NotNull(handlerData, nameof(handlerData));

            Variables runtimeVariables = ExecutionContext.Variables;
            IStepHost stepHost = HostContext.CreateService<IDefaultStepHost>();
            // Setup container stephost and the right runtime variables for running job inside container.
            if (ExecutionContext.Container != null)
            {
                if (handlerData is ContainerActionHandlerData)
                {
                    // plugin handler always runs on the Host, the rumtime variables needs to the variable works on the Host, ex: file path variable System.DefaultWorkingDirectory
                    Dictionary<string, VariableValue> variableCopy = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
                    foreach (var publicVar in ExecutionContext.Variables.Public)
                    {
                        variableCopy[publicVar.Key] = new VariableValue(ExecutionContext.Container.TranslateToHostPath(publicVar.Value));
                    }
                    foreach (var secretVar in ExecutionContext.Variables.Private)
                    {
                        variableCopy[secretVar.Key] = new VariableValue(ExecutionContext.Container.TranslateToHostPath(secretVar.Value), true);
                    }

                    List<string> expansionWarnings;
                    runtimeVariables = new Variables(HostContext, variableCopy, out expansionWarnings);
                    expansionWarnings?.ForEach(x => ExecutionContext.Warning(x));
                }
                else if (handlerData is NodeScriptActionHandlerData)
                {
                    // Only the node, node10, and powershell3 handlers support running inside container. 
                    // Make sure required container is already created.
                    ArgUtil.NotNullOrEmpty(ExecutionContext.Container.ContainerId, nameof(ExecutionContext.Container.ContainerId));
                    var containerStepHost = HostContext.CreateService<IContainerStepHost>();
                    containerStepHost.Container = ExecutionContext.Container;
                    stepHost = containerStepHost;
                }
                else
                {
                    throw new NotSupportedException(String.Format("Task '{0}' is using legacy execution handler '{1}' which is not supported in container execution flow.", definition.Data.FriendlyName, handlerData.GetType().ToString()));
                }
            }


            // Load the inputs.
            ExecutionContext.Output($"{WellKnownTags.Debug}Loading inputs");
            var templateTrace = ExecutionContext.ToTemplateTraceWriter();
            var schema = new PipelineTemplateSchemaFactory().CreateSchema();
            var templateEvaluator = new PipelineTemplateEvaluator(templateTrace, schema);
            var inputs = templateEvaluator.EvaluateStepInputs(Action.Inputs, ExecutionContext.ExpressionValues);

            // Merge the default inputs from the definition
            foreach (var input in (definition.Data?.Inputs ?? new TaskInputDefinition[0]))
            {
                string key = input.Name?.Trim();
                if (!string.IsNullOrEmpty(key) && !inputs.ContainsKey(key))
                {
                    inputs[key] = input.DefaultValue?.Trim() ?? string.Empty;
                }
            }

            // Translate the server file path inputs to local paths.
            foreach (var input in definition.Data?.Inputs ?? new TaskInputDefinition[0])
            {
                if (string.Equals(input.InputType, TaskInputType.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    Trace.Verbose($"Translating file path input '{input.Name}': '{inputs[input.Name]}'");
                    inputs[input.Name] = stepHost.ResolvePathForStepHost(TranslateFilePathInput(inputs[input.Name] ?? string.Empty));
                    Trace.Verbose($"Translated file path input '{input.Name}': '{inputs[input.Name]}'");
                }
            }

            // Load the task environment.
            ExecutionContext.Output($"{WellKnownTags.Debug}Loading env");
            var environment = templateEvaluator.EvaluateStepEnvironment(Action.Environment, ExecutionContext.ExpressionValues, VarUtil.EnvironmentVariableKeyComparer);

            // Create the handler.
            IHandler handler = handlerFactory.Create(
                ExecutionContext,
                Action.Reference,
                stepHost,
                handlerData,
                inputs,
                environment,
                runtimeVariables,
                taskDirectory: definition.Directory);

            // Run the task.
            await handler.RunAsync();
        }

        private void PrintTaskMetaData(Definition actionDefinition)
        {
            ArgUtil.NotNull(Action, nameof(Action));
            ArgUtil.NotNull(Action.Reference, nameof(Action.Reference));
            ArgUtil.NotNull(actionDefinition.Data, nameof(actionDefinition.Data));

            ExecutionContext.Output("==============================================================================");
            ExecutionContext.Output($"Action             : {actionDefinition.Data.FriendlyName}");
            ExecutionContext.Output($"Description        : {actionDefinition.Data.Description}");

            if (Action.Reference.Type == Pipelines.ActionSourceType.ContainerRegistry)
            {
                var registryAction = Action.Reference as Pipelines.ContainerRegistryActionDefinitionReference;
                var image = ExecutionContext.Containers.Single(x => x.Alias == registryAction.Container).Image;
                ExecutionContext.Output($"Action image       : {image}");
            }
            else
            {
                var repoAction = Action.Reference as Pipelines.RepositoryActionDefinitionReference;
                var repo = ExecutionContext.Repositories.Single(x => x.Alias == repoAction.Repository).Id;
                var version = ExecutionContext.Repositories.Single(x => x.Alias == repoAction.Repository).Version;
                ExecutionContext.Output($"Action repository  : {repo}@{version}");
            }

            ExecutionContext.Output($"Author             : {actionDefinition.Data.Author}");
            ExecutionContext.Output("==============================================================================");
        }

        private string TranslateFilePathInput(string inputValue)
        {
            Trace.Entering();

#if OS_WINDOWS
            if (!string.IsNullOrEmpty(inputValue))
            {
                Trace.Verbose("Trim double quotes around filepath type input on Windows.");
                inputValue = inputValue.Trim('\"');

                Trace.Verbose($"Replace any '{Path.AltDirectorySeparatorChar}' with '{Path.DirectorySeparatorChar}'.");
                inputValue = inputValue.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
#endif 
            // if inputValue is rooted, return full path.
            string fullPath;
            if (!string.IsNullOrEmpty(inputValue) &&
                inputValue.IndexOfAny(Path.GetInvalidPathChars()) < 0 &&
                Path.IsPathRooted(inputValue))
            {
                try
                {
                    fullPath = Path.GetFullPath(inputValue);
                    Trace.Info($"The original input is a rooted path, return absolute path: {fullPath}");
                    return fullPath;
                }
                catch (Exception ex)
                {
                    Trace.Error(ex);
                    Trace.Info($"The original input is a rooted path, but it is not full qualified, return the path: {inputValue}");
                    return inputValue;
                }
            }

            // use jobextension solve inputValue, if solved result is rooted, return full path.
            var extensionManager = HostContext.GetService<IExtensionManager>();
            IJobExtension[] extensions =
                (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                .Where(x => x.HostType.HasFlag(ExecutionContext.Variables.System_HostType))
                .ToArray();
            foreach (IJobExtension extension in extensions)
            {
                fullPath = extension.GetRootedPath(ExecutionContext, inputValue);
                if (!string.IsNullOrEmpty(fullPath))
                {
                    // Stop on the first path root found.
                    Trace.Info($"{extension.HostType.ToString()} JobExtension resolved a rooted path:: {fullPath}");
                    return fullPath;
                }
            }

            // return original inputValue.
            Trace.Info("Can't root path even by using JobExtension, return original input.");
            return inputValue;
        }
    }
}
