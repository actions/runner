using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Handlers;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public enum JobRunStage
    {
        PreJob,
        Main,
        PostJob,
    }

    [ServiceLocator(Default = typeof(TaskRunner))]
    public interface ITaskRunner : IStep, IAgentService
    {
        JobRunStage Stage { get; set; }
        TaskInstance TaskInstance { get; set; }
    }

    public sealed class TaskRunner : AgentService, ITaskRunner
    {
        public JobRunStage Stage { get; set; }

        public INode Condition { get; set; }

        public bool ContinueOnError => TaskInstance?.ContinueOnError ?? default(bool);

        public string DisplayName => TaskInstance?.DisplayName;

        public bool Enabled => TaskInstance?.Enabled ?? default(bool);

        public IExecutionContext ExecutionContext { get; set; }

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

            ExecutionData currentExecution = null;
            switch (Stage)
            {
                case JobRunStage.PreJob:
                    currentExecution = definition.Data?.PreJobExecution;
                    break;
                case JobRunStage.Main:
                    currentExecution = definition.Data?.Execution;
                    break;
                case JobRunStage.PostJob:
                    currentExecution = definition.Data?.PostJobExecution;
                    break;
            };

            if ((currentExecution?.All.Any(x => x is PowerShell3HandlerData)).Value &&
                (currentExecution?.All.Any(x => x is PowerShellHandlerData && x.Platforms != null && x.Platforms.Contains("windows", StringComparer.OrdinalIgnoreCase))).Value)
            {
                // When task contains both PS and PS3 implementations, we will always prefer PS3 over PS regardless of the platform pinning.
                Trace.Info("Ignore platform pinning for legacy PowerShell execution handler.");
                var legacyPShandler = currentExecution?.All.Where(x => x is PowerShellHandlerData).FirstOrDefault();
                legacyPShandler.Platforms = null;
            }

            HandlerData handlerData =
                currentExecution?.All
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

            // Get each endpoint ID referenced by the task.
            var endpointIds = new List<Guid>();
            foreach (var input in definition.Data?.Inputs ?? new TaskInputDefinition[0])
            {
                if ((input.InputType ?? string.Empty).StartsWith("connectedService:", StringComparison.OrdinalIgnoreCase))
                {
                    string inputKey = input?.Name?.Trim() ?? string.Empty;
                    string inputValue;
                    if (!string.IsNullOrEmpty(inputKey) &&
                        inputs.TryGetValue(inputKey, out inputValue) &&
                        !string.IsNullOrEmpty(inputValue))
                    {
                        foreach (string rawId in inputValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            Guid parsedId;
                            if (Guid.TryParse(rawId.Trim(), out parsedId) && parsedId != Guid.Empty)
                            {
                                endpointIds.Add(parsedId);
                            }
                        }
                    }
                }
            }

            // Get the endpoints referenced by the task.
            var endpoints = (ExecutionContext.Endpoints ?? new List<ServiceEndpoint>(0))
                .Join(inner: endpointIds,
                    outerKeySelector: (ServiceEndpoint endpoint) => endpoint.Id,
                    innerKeySelector: (Guid endpointId) => endpointId,
                    resultSelector: (ServiceEndpoint endpoint, Guid endpointId) => endpoint)
                .ToList();

            // Add the system endpoint.
            foreach (ServiceEndpoint endpoint in (ExecutionContext.Endpoints ?? new List<ServiceEndpoint>(0)))
            {
                if (string.Equals(endpoint.Name, ServiceEndpoints.SystemVssConnection, StringComparison.OrdinalIgnoreCase))
                {
                    endpoints.Add(endpoint);
                    break;
                }
            }

            // Get each secure file ID referenced by the task.
            var secureFileIds = new List<Guid>();
            foreach (var input in definition.Data?.Inputs ?? new TaskInputDefinition[0])
            {
                if (string.Equals(input.InputType ?? string.Empty, "secureFile", StringComparison.OrdinalIgnoreCase))
                {
                    string inputKey = input?.Name?.Trim() ?? string.Empty;
                    string inputValue;
                    if (!string.IsNullOrEmpty(inputKey) &&
                        inputs.TryGetValue(inputKey, out inputValue) &&
                        !string.IsNullOrEmpty(inputValue))
                    {
                        foreach (string rawId in inputValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            Guid parsedId;
                            if (Guid.TryParse(rawId.Trim(), out parsedId) && parsedId != Guid.Empty)
                            {
                                secureFileIds.Add(parsedId);
                            }
                        }
                    }
                }
            }

            // Get the endpoints referenced by the task.
            var secureFiles = (ExecutionContext.SecureFiles ?? new List<SecureFile>(0))
                .Join(inner: secureFileIds,
                    outerKeySelector: (SecureFile secureFile) => secureFile.Id,
                    innerKeySelector: (Guid secureFileId) => secureFileId,
                    resultSelector: (SecureFile secureFile, Guid secureFileId) => secureFile)
                .ToList();

            // Set output variables.
            foreach (var outputVar in definition.Data?.OutputVariables ?? new OutputVariable[0])
            {
                if (outputVar != null && !string.IsNullOrEmpty(outputVar.Name))
                {
                    ExecutionContext.OutputVariables.Add(outputVar.Name);
                }
            }

            // Create the handler.
            IHandler handler = handlerFactory.Create(
                ExecutionContext,
                endpoints,
                secureFiles,
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
