using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public string Condition
        {
            get
            {
                if (ExecutionContext.Features.HasFlag(PlanFeatures.TaskCondition) || !string.IsNullOrEmpty(ExecutionContext.Variables.Get("VSTS_TEMP_FEATURE_TASKCONDITION")))
                {
                    return ExecutionContext.Variables.Get($"VSTS_TEMP_CONDITION_{DisplayName}"); //taskInstance.Condition;
                }
                else
                {
                    return TaskInstance.AlwaysRun ? $"{Constants.Expressions.SucceededOrFailed}()" : $"{Constants.Expressions.Succeeded}()";
                }
            }
        }

        public bool ContinueOnError => TaskInstance?.ContinueOnError ?? default(bool);

        public bool Critical => false;

        public string DisplayName => TaskInstance?.DisplayName;

        public bool Enabled => TaskInstance?.Enabled ?? default(bool);

        public IExecutionContext ExecutionContext { get; set; }

        public bool Finally => false;

        public TaskInstance TaskInstance { get; set; }

        public TimeSpan? Timeout => (TaskInstance?.TimeoutInMinutes ?? 0) > 0 ? (TimeSpan?)TimeSpan.FromMinutes(TaskInstance.TimeoutInMinutes) : null;

        public async Task RunAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(ExecutionContext.Variables, nameof(ExecutionContext.Variables));
            ArgUtil.NotNull(TaskInstance, nameof(TaskInstance));
            var taskManager = HostContext.GetService<ITaskManager>();
            var handlerFactory = HostContext.GetService<IHandlerFactory>();

            // Set the task display name variable.
            ExecutionContext.Variables.Set(Constants.Variables.Task.DisplayName, DisplayName);

            // Load the task definition and choose the handler.
            // TODO: Add a try catch here to give a better error message.
            Definition definition = taskManager.Load(TaskInstance);
            ArgUtil.NotNull(definition, nameof(definition));

            // Print out task metadata
            PrintTaskMetaData(definition);

            if ((definition.Data?.Execution?.All.Any(x => x is PowerShell3HandlerData)).Value &&
                (definition.Data?.Execution?.All.Any(x => x is PowerShellHandlerData && x.Platforms != null && x.Platforms.Contains("windows", StringComparer.OrdinalIgnoreCase))).Value)
            {
                // When task contains both PS and PS3 implementations, we will always prefer PS3 over PS regardless of the platform pinning.
                Trace.Info("Ignore platform pinning for legacy PowerShell execution handler.");
                var legacyPShandler = definition.Data?.Execution?.All.Where(x => x is PowerShellHandlerData).FirstOrDefault();
                legacyPShandler.Platforms = null;
            }

            HandlerData handlerData =
                definition.Data?.Execution?.All
                .OrderBy(x => !x.PreferredOnCurrentPlatform()) // Sort true to false.
                .ThenBy(x => x.Priority)
                .FirstOrDefault();
            if (handlerData == null)
            {
                throw new Exception(StringUtil.Loc("SupportedTaskHandlerNotFound"));
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
            VarUtil.ExpandEnvironmentVariables(HostContext, target: inputs);

            // Translate the server file path inputs to local paths.
            foreach (var input in definition.Data?.Inputs ?? new TaskInputDefinition[0])
            {
                if (string.Equals(input.InputType, TaskInputType.FilePath, StringComparison.OrdinalIgnoreCase))
                {
                    Trace.Verbose($"Translating file path input '{input.Name}': '{inputs[input.Name]}'");
                    inputs[input.Name] = TranslateFilePathInput(inputs[input.Name] ?? string.Empty);
                    Trace.Verbose($"Translated file path input '{input.Name}': '{inputs[input.Name]}'");
                }
            }

            // Expand the handler inputs.
            Trace.Verbose("Expanding handler inputs.");
            VarUtil.ExpandValues(HostContext, source: inputs, target: handlerData.Inputs);
            ExecutionContext.Variables.ExpandValues(target: handlerData.Inputs);

            // Create the handler.
            IHandler handler = handlerFactory.Create(
                ExecutionContext,
                handlerData,
                inputs,
                taskDirectory: definition.Directory,
                filePathInputRootDirectory: TranslateFilePathInput(string.Empty));

            // Run the task.
            await handler.RunAsync();
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
                .Where(x => string.Equals(x.HostType, ExecutionContext.Variables.System_HostType, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            foreach (IJobExtension extension in extensions)
            {
                fullPath = extension.GetRootedPath(ExecutionContext, inputValue);
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

        private void PrintTaskMetaData(Definition taskDefinition)
        {
            ArgUtil.NotNull(TaskInstance, nameof(TaskInstance));
            ArgUtil.NotNull(taskDefinition.Data, nameof(taskDefinition.Data));
            ExecutionContext.Output("==============================================================================");
            ExecutionContext.Output($"Task         : {taskDefinition.Data.FriendlyName}");
            ExecutionContext.Output($"Description  : {taskDefinition.Data.Description}");
            ExecutionContext.Output($"Version      : {TaskInstance.Version}");
            ExecutionContext.Output($"Author       : {taskDefinition.Data.Author}");
            ExecutionContext.Output($"Help         : {taskDefinition.Data.HelpMarkDown}");
            ExecutionContext.Output("==============================================================================");
        }
    }
}
