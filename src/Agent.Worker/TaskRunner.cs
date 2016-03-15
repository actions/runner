using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(TaskRunner))]
    public interface ITaskRunner : IStep, IAgentService
    {
        TaskInstance TaskInstance { get; set; }
    }

    public sealed class TaskRunner : AgentService, ITaskRunner
    {
        public bool AlwaysRun => TaskInstance?.AlwaysRun ?? default(bool);
        public bool ContinueOnError => TaskInstance?.ContinueOnError ?? default(bool);
        public bool Critical => false;
        public string DisplayName => TaskInstance?.DisplayName;
        public bool Enabled => TaskInstance?.Enabled ?? default(bool);
        public IExecutionContext ExecutionContext { get; set; }
        public bool Finally => false;
        public TaskInstance TaskInstance { get; set; }

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(ExecutionContext.Variables, nameof(ExecutionContext.Variables));
            ArgUtil.NotNull(TaskInstance, nameof(TaskInstance));
            var taskManager = HostContext.GetService<ITaskManager>();
            var handlerFactory = HostContext.GetService<IHandlerFactory>();

            // Load the task definition and choose the handler.
            // TODO: Add a try catch here to give a better error message.
            Definition definition = taskManager.Load(TaskInstance);
            ArgUtil.NotNull(definition, nameof(definition));
            HandlerData handlerData =
                definition.Data?.Execution?.All
                .OrderBy(x => !x.PreferredOnCurrentPlatform()) // Sort true to false.
                .ThenBy(x => x.Priority)
                .FirstOrDefault();
            if (handlerData == null)
            {
                // TODO: BETTER ERROR AND LOC
                throw new Exception("Supported handler not found.");
            }

            // Load the default input values from the definition.
            Trace.Verbose("Loading default inputs.");
            var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var input in (definition.Data?.Inputs ?? new TaskInputDefinition[0]))
            {
                string key = input?.Name?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(key))
                {
                    inputs[key] = input.DefaultValue?.Trim() ?? string.Empty;
                }
            }

            // Merge the instance inputs.
            Trace.Verbose("Loading instance inputs.");
            foreach (var input in (TaskInstance.Inputs as IEnumerable<KeyValuePair<string, string>> ?? new KeyValuePair<string, string>[0]))
            {
                string key = input.Key?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(key))
                {
                    inputs[key] = input.Value?.Trim() ?? string.Empty;
                }
            }

            // Expand the inputs.
            Trace.Verbose("Expanding inputs.");
            foreach (string inputKey in inputs.Keys.ToArray())
            {
                // Bump the start index with each replacement to prevent recursive replacement.
                Trace.Verbose($"Expanding input '{inputKey}'.");
                int startIndex = 0;
                int prefixIndex;
                int suffixIndex;
                string inputValue = inputs[inputKey] ?? string.Empty;
                while (startIndex < inputValue.Length &&
                    (prefixIndex = inputValue.IndexOf(Constants.Variables.MacroPrefix, startIndex, StringComparison.Ordinal)) >= 0 &&
                    (suffixIndex = inputValue.IndexOf(Constants.Variables.MacroSuffix, prefixIndex + Constants.Variables.MacroPrefix.Length, StringComparison.Ordinal)) >= 0)
                {
                    // A variable macro candidate was found.
                    string variableKey = inputValue.Substring(
                        startIndex: prefixIndex + Constants.Variables.MacroPrefix.Length,
                        length: suffixIndex - prefixIndex - Constants.Variables.MacroPrefix.Length);
                    Trace.Verbose($"Variable macro candidate '{variableKey}'.");
                    string variableValue;
                    if (!string.IsNullOrEmpty(variableKey) &&
                        ExecutionContext.Variables.TryGetValue(variableKey, out variableValue))
                    {
                        // Update the input value.
                        Trace.Verbose("Candidate found.");
                        inputValue = string.Concat(
                            inputValue.Substring(0, prefixIndex),
                            variableValue ?? string.Empty,
                            inputValue.Substring(suffixIndex + Constants.Variables.MacroSuffix.Length));
                        startIndex = prefixIndex + (variableValue ?? string.Empty).Length;
                    }
                    else
                    {
                        Trace.Verbose("Candidate not found.");
                        startIndex += Constants.Variables.MacroPrefix.Length;
                    }
                }

                inputs[inputKey] = inputValue;
            }

            // TODO: Delegate to the source provider to fixup the file path inputs.

            // Create the handler.
            IHandler handler = handlerFactory.Create(
                ExecutionContext,
                handlerData,
                inputs,
                taskDirectory: definition.Directory);

            // Run the task.
            await handler.RunAsync();
        }
    }
}
