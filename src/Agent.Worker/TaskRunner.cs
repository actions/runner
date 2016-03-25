using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
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
            ExecutionContext.Variables.ExpandValues(target: inputs);

            // Delegate to the JobExtension to fixup the file path inputs.
            foreach (var input in definition.Data?.Inputs ?? new TaskInputDefinition[0])
            {
                if (String.Equals(input.InputType, TaskInputType.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    Trace.Verbose($"Expanding filepath type input {input.Name}: {inputs[input.Name] ?? string.Empty}.");
                    inputs[input.Name] = ExpandFilePathInput(inputs[input.Name] ?? string.Empty);
                    Trace.Verbose($"Expanded filepath type input {input.Name}: {inputs[input.Name] ?? string.Empty}.");
                }
            }

            // Create the handler.
            IHandler handler = handlerFactory.Create(
                ExecutionContext,
                handlerData,
                inputs,
                taskDirectory: definition.Directory);

            // Run the task.
            await handler.RunAsync();
        }

        private string ExpandFilePathInput(string inputValue)
        {
            Trace.Entering();

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
                    Trace.Info($"The original input is a rooted path, but it is not a full qualified path: {inputValue}");
                    Trace.Error(ex);
                }
            }

            // use jobextension solve inputValue, if solved result is rooted, return full path.
            var extensionManager = HostContext.GetService<IExtensionManager>();
            IJobExtension[] extensions =
                (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                .Where(x => string.Equals(x.HostType, ExecutionContext.Variables.System_HostType, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (IJobExtension extension in extensions)
            {
                extension.GetRootedPath(ExecutionContext, inputValue, out fullPath);
                if (!string.IsNullOrEmpty(fullPath))
                {
                    // Stop on the first path root found.
                    Trace.Info($"{extension.HostType} JobExtension resolved a rooted path:: {fullPath}");
                    return fullPath;
                }
            }

            // return original inputValue.
            Trace.Info("Can't root path even by using JobExtension, return original input.");
            return inputValue;
        }
    }
}
