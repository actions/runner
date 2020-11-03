using System;
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
    public enum ActionRunStage
    {
        Pre,
        Main,
        Post,
    }

    [ServiceLocator(Default = typeof(ActionRunner))]
    public interface IActionRunner : IStep, IRunnerService
    {
        ActionRunStage Stage { get; set; }
        bool TryEvaluateDisplayName(DictionaryContextData contextData, IExecutionContext context);
        Pipelines.ActionStep Action { get; set; }
    }

    public sealed class ActionRunner : RunnerService, IActionRunner
    {
        private bool _didFullyEvaluateDisplayName = false;

        private string _displayName;

        public ActionRunStage Stage { get; set; }

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
            set
            {
                _displayName = value;
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

            if (handlerData.HasPre &&
                Action.Reference is Pipelines.RepositoryPathReference repoAction &&
                string.Equals(repoAction.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                ExecutionContext.Warning($"`pre` execution is not supported for local action from '{repoAction.Path}'");
            }

            // The action has post cleanup defined.
            // we need to create timeline record for them and add them to the step list that StepRunner is using
            if (handlerData.HasPost && (Stage == ActionRunStage.Pre || Stage == ActionRunStage.Main))
            {
                string postDisplayName = $"Post {this.DisplayName}";
                if (Stage == ActionRunStage.Pre &&
                    this.DisplayName.StartsWith("Pre ", StringComparison.OrdinalIgnoreCase))
                {
                    // Trim the leading `Pre ` from the display name.
                    // Otherwise, we will get `Post Pre xxx` as DisplayName for the Post step.
                    postDisplayName = $"Post {this.DisplayName.Substring("Pre ".Length)}";
                }
                var repositoryReference = Action.Reference as RepositoryPathReference;
                var pathString = string.IsNullOrEmpty(repositoryReference.Path) ? string.Empty : $"/{repositoryReference.Path}";
                var repoString = string.IsNullOrEmpty(repositoryReference.Ref) ? $"{repositoryReference.Name}{pathString}" :
                    $"{repositoryReference.Name}{pathString}@{repositoryReference.Ref}";

                ExecutionContext.Debug($"Register post job cleanup for action: {repoString}");

                var actionRunner = HostContext.CreateService<IActionRunner>();
                actionRunner.Action = Action;
                actionRunner.Stage = ActionRunStage.Post;
                actionRunner.Condition = handlerData.CleanupCondition;
                actionRunner.DisplayName = postDisplayName;

                ExecutionContext.RegisterPostJobStep(actionRunner);
            }

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

            // Set GITHUB_ACTION_REPOSITORY if this Action is from a repository
            if (Action.Reference is Pipelines.RepositoryPathReference repoPathReferenceAction &&
                !string.Equals(repoPathReferenceAction.RepositoryType, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                ExecutionContext.SetGitHubContext("action_repository", repoPathReferenceAction.Name);
                ExecutionContext.SetGitHubContext("action_ref", repoPathReferenceAction.Ref);
            }

            // Setup container stephost for running inside the container.
            if (ExecutionContext.Global.Container != null)
            {
                // Make sure required container is already created.
                ArgUtil.NotNullOrEmpty(ExecutionContext.Global.Container.ContainerId, nameof(ExecutionContext.Global.Container.ContainerId));
                var containerStepHost = HostContext.CreateService<IContainerStepHost>();
                containerStepHost.Container = ExecutionContext.Global.Container;
                stepHost = containerStepHost;
            }

            // Setup File Command Manager
            var fileCommandManager = HostContext.CreateService<IFileCommandManager>();
            fileCommandManager.InitializeFiles(ExecutionContext, null);

            // Load the inputs.
            ExecutionContext.Debug("Loading inputs");
            var templateEvaluator = ExecutionContext.ToPipelineTemplateEvaluator();
            var inputs = templateEvaluator.EvaluateStepInputs(Action.Inputs, ExecutionContext.ExpressionValues, ExecutionContext.ExpressionFunctions);

            var userInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> input in inputs)
            {
                userInputs.Add(input.Key);
                string message = "";
                if (definition.Data?.Deprecated?.TryGetValue(input.Key, out message) == true)
                {
                    ExecutionContext.Warning(String.Format("Input '{0}' has been deprecated with message: {1}", input.Key, message));
                }
            }

            var validInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (handlerData.ExecutionType == ActionExecutionType.Container)
            {
                // container action always accept 'entryPoint' and 'args' as inputs
                // https://help.github.com/en/actions/reference/workflow-syntax-for-github-actions#jobsjob_idstepswithargs
                validInputs.Add("entryPoint");
                validInputs.Add("args");
            }
            // Merge the default inputs from the definition
            if (definition.Data?.Inputs != null)
            {
                var manifestManager = HostContext.GetService<IActionManifestManager>();
                foreach (var input in definition.Data.Inputs)
                {
                    string key = input.Key.AssertString("action input name").Value;
                    validInputs.Add(key);
                    if (!inputs.ContainsKey(key))
                    {
                        inputs[key] = manifestManager.EvaluateDefaultInput(ExecutionContext, key, input.Value);
                    }
                }
            }

            // Validate inputs only for actions with action.yml
            if (Action.Reference.Type == Pipelines.ActionSourceType.Repository)
            {
                var unexpectedInputs = new List<string>();
                foreach (var input in userInputs)
                {
                    if (!validInputs.Contains(input))
                    {
                        unexpectedInputs.Add(input);
                    }
                }

                if (unexpectedInputs.Count > 0)
                {
                    ExecutionContext.Warning($"Unexpected input(s) '{string.Join("', '", unexpectedInputs)}', valid inputs are ['{string.Join("', '", validInputs)}']");
                }
            }

            // Load the action environment.
            ExecutionContext.Debug("Loading env");
            var environment = new Dictionary<String, String>(VarUtil.EnvironmentVariableKeyComparer);

#if OS_WINDOWS
            var envContext = ExecutionContext.ExpressionValues["env"] as DictionaryContextData;
#else
            var envContext = ExecutionContext.ExpressionValues["env"] as CaseSensitiveDictionaryContextData;
#endif
            // Apply environment from env context, env context contains job level env and action's evn block
            foreach (var env in envContext)
            {
                environment[env.Key] = env.Value.ToString();
            }

            // Apply action's intra-action state at last
            foreach (var state in ExecutionContext.IntraActionState)
            {
                environment[$"STATE_{state.Key}"] = state.Value ?? string.Empty;
            }

            // Create the handler.
            IHandler handler = handlerFactory.Create(
                            ExecutionContext,
                            Action.Reference,
                            stepHost,
                            handlerData,
                            inputs,
                            environment,
                            ExecutionContext.Global.Variables,
                            actionDirectory: definition.Directory);

            // Print out action details
            handler.PrintActionDetails(Stage);

            // Run the task.
            try 
            {
                await handler.RunAsync(Stage);
            }
            finally 
            {
                fileCommandManager.ProcessFiles(ExecutionContext, ExecutionContext.Global.Container);
            }

        }

        public bool TryEvaluateDisplayName(DictionaryContextData contextData, IExecutionContext context)
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
            _displayName = GenerateDisplayName(Action, contextData, context, out didFullyEvaluate);

            // If we evaluated fully mask any secrets
            if (didFullyEvaluate)
            {
                _displayName = HostContext.SecretMasker.MaskSecrets(_displayName);
            }
            context.Debug($"Set step '{Action.Name}' display name to: '{_displayName}'");
            _didFullyEvaluateDisplayName = didFullyEvaluate;
            return didFullyEvaluate;
        }

        private string GenerateDisplayName(ActionStep action, DictionaryContextData contextData, IExecutionContext context, out bool didFullyEvaluate)
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
            try
            {
                if (tokenToParse.CheckHasRequiredContext(contextData, context.ExpressionFunctions))
                {
                    var templateEvaluator = context.ToPipelineTemplateEvaluator();
                    displayName = templateEvaluator.EvaluateStepDisplayName(tokenToParse, contextData, context.ExpressionFunctions);
                    didFullyEvaluate = true;
                }
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
