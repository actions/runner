﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Worker.Handlers;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using System.Collections.Generic;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionRunner))]
    public interface IActionRunner : IStep, IRunnerService
    {
        Boolean TryEvaluateDisplayName(IDictionary<String, PipelineContextData> contextData, IExecutionContext context);
        Pipelines.ActionStep Action { get; set; }
    }

    public sealed class ActionRunner : RunnerService, IActionRunner
    {
        private bool _didFullyEvaluateDisplayName = false;

        private string _displayName;

        public string Condition { get; set; }

        public TemplateToken ContinueOnError => Action?.ContinueOnError;

        public string DisplayName
        {
            get 
            {
                // TODO: remove the Action.DisplayName check post m158 deploy, it is done for back compat for older servers
                if (!string.IsNullOrEmpty(Action?.DisplayName))
                {
                    return Action?.DisplayName;
                }
                return string.IsNullOrEmpty(_displayName) ? "run" : _displayName;
            }
        }

        public IExecutionContext ExecutionContext { get; set; }

        public Pipelines.ActionStep Action { get; set; }

        public TemplateToken Timeout => Action?.TimeoutInMinutes;

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

            foreach (KeyValuePair<string, string> input in inputs)
            {
                string message = "";
                if (definition.Data?.Deprecated?.TryGetValue(input.Key, out message)==true)
                {
                    ExecutionContext.Warning(String.Format("Input '{0}' has been deprecated with message: {1}", input.Key, message));
                }
            }

            // Merge the default inputs from the definition
            if (definition.Data?.Inputs != null)
            {
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
            var environment = new Dictionary<String, String>(VarUtil.EnvironmentVariableKeyComparer);

            // Apply environment set using ##[set-env] first since these are job level env
            foreach (var env in ExecutionContext.EnvironmentVariables)
            {
                environment[env.Key] = env.Value ?? string.Empty;
            }

            // Apply action's env block later.
            var actionEnvironment = templateEvaluator.EvaluateStepEnvironment(Action.Environment, ExecutionContext.ExpressionValues, VarUtil.EnvironmentVariableKeyComparer);
            foreach (var env in actionEnvironment)
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

        public bool TryEvaluateDisplayName(IDictionary<String, PipelineContextData> contextData, IExecutionContext context)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(Action, nameof(Action));

            // If we have already expanded the display name, there is no need to expand it again
            // TODO: Remove the ShouldEvaluateDisplayName check and field post m158 deploy, we should do it by default once the server is updated
            if (_didFullyEvaluateDisplayName || !string.IsNullOrEmpty(Action.DisplayName))
            {
                return false;
            }

            bool didFullyEvaluate;
            _displayName = ActionRunner.GenerateDisplayName(Action, contextData, context, out didFullyEvaluate);

            // If we evaluated fully mask any secrets
            if (didFullyEvaluate)
            {
                _displayName = HostContext.SecretMasker.MaskSecrets(_displayName);
            }
            context.Debug($"Set step '{Action.Name}' display name to: '{_displayName}'");
            _didFullyEvaluateDisplayName = didFullyEvaluate;
            return didFullyEvaluate;
        }

        private static string GenerateDisplayName(ActionStep action, IDictionary<String, PipelineContextData> contextData, IExecutionContext context, out bool didFullyEvaluate)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(action, nameof(action));

            var displayName = string.Empty;
            var prefix = string.Empty;
            var tokenToParse = default(ScalarToken);
            didFullyEvaluate = false;
            // Get the token we need to parse
            // It could be passed in as the Display Name, or we have to pull it from various parts of the Action.
            if (action.DisplayNameToken != null)
            {
                tokenToParse = action.DisplayNameToken as ScalarToken;
            }
            else if (action.Reference?.Type == ActionSourceType.Repository)
            {
                prefix = PipelineTemplateConstants.RunDisplayPrefix;
                var repositoryReference = action.Reference as RepositoryPathReference;
                var pathString = string.IsNullOrEmpty(repositoryReference.Path) ? string.Empty : $"/{repositoryReference.Path}";
                var repoString = string.IsNullOrEmpty(repositoryReference.Ref) ? $"{repositoryReference.Name}{pathString}" :
                    $"{repositoryReference.Name}{pathString}@{repositoryReference.Ref}";
                tokenToParse = new StringToken(null, null, null, repoString);
            } 
            else if (action.Reference?.Type == ActionSourceType.ContainerRegistry)
            {
                prefix = PipelineTemplateConstants.RunDisplayPrefix;
                var containerReference = action.Reference as ContainerRegistryReference;
                tokenToParse = new StringToken(null, null, null, containerReference.Image);
            }
            else if (action.Reference?.Type == ActionSourceType.AgentPlugin)
            {
                prefix = PipelineTemplateConstants.RunDisplayPrefix;
                var pluginReference = action.Reference as PluginReference;
                tokenToParse = new StringToken(null, null, null, pluginReference.Plugin);
            }
            else if (action.Reference?.Type == ActionSourceType.Script)
            {
                prefix = PipelineTemplateConstants.RunDisplayPrefix;
                var inputs = action.Inputs.AssertMapping(null);
                foreach (var pair in inputs)
                {
                    var propertyName = pair.Key.AssertString($"{PipelineTemplateConstants.Steps}");
                    if (string.Equals(propertyName.Value, "script", StringComparison.OrdinalIgnoreCase))
                    {
                        tokenToParse = pair.Value.AssertScalar($"{PipelineTemplateConstants.Steps} item {PipelineTemplateConstants.Run}");
                        break;
                    }
                }
            }
            else 
            {
                context.Error($"Encountered an unknown action reference type when evaluating the display name: {action.Reference?.Type}");
                return displayName;
            }

            // If we have nothing to parse, abort
            if (tokenToParse == null)
            {
                return displayName;
            }
            // Try evaluating fully
            var schema = new PipelineTemplateSchemaFactory().CreateSchema();
            var templateEvaluator = new PipelineTemplateEvaluator(context.ToTemplateTraceWriter(), schema);
            try 
            {
                didFullyEvaluate = templateEvaluator.TryEvaluateStepDisplayName(tokenToParse, contextData, out displayName);  
            }
            catch (TemplateValidationException e)
            {
                context.Warning($"Encountered an error when evaluating display name {tokenToParse.ToString()}. {e.Message}");
                return displayName;
            }

            // Default to a prettified token if we could not evaluate
            if (!didFullyEvaluate)
            {
                displayName = tokenToParse.ToDisplayString();
            }

            displayName = FormatStepName(prefix, displayName);
            return displayName;
        }

        private static string FormatStepName(string prefix, string stepName)
        {
            if (string.IsNullOrEmpty(stepName))
            {
                return string.Empty;
            }

            var result = stepName.TrimStart(' ', '\t', '\r', '\n');
            var firstNewLine = result.IndexOfAny(new[] { '\r', '\n' });
            if (firstNewLine >= 0)
            {
                result = result.Substring(0, firstNewLine);
            }
            return $"{prefix}{result}";
        }
    }
}
