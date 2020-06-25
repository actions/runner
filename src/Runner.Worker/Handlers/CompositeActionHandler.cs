using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using Pipelines = GitHub.DistributedTask.Pipelines;


namespace GitHub.Runner.Worker.Handlers
{
    [ServiceLocator(Default = typeof(CompositeActionHandler))]
    public interface ICompositeActionHandler : IHandler
    {
        CompositeActionExecutionData Data { get; set; }
    }
    public sealed class CompositeActionHandler : Handler, ICompositeActionHandler
    {
        public CompositeActionExecutionData Data { get; set; }

        private void InitializeScope(IStep step, Dictionary<string, PipelineContextData> scopeInputs)
        {
            var executionContext = step.ExecutionContext;
            var stepsContext = executionContext.StepsContext;
            if (!string.IsNullOrEmpty(executionContext.ScopeName))
            {
                // Gather uninitialized current and ancestor scopes
                var scope = executionContext.Scopes[executionContext.ScopeName];
                var scopesToInitialize = default(Stack<Pipelines.ContextScope>);
                while (scope != null && !scopeInputs.ContainsKey(scope.Name))
                {
                    if (scopesToInitialize == null)
                    {
                        scopesToInitialize = new Stack<Pipelines.ContextScope>();
                    }
                    scopesToInitialize.Push(scope);
                    scope = string.IsNullOrEmpty(scope.ParentName) ? null : executionContext.Scopes[scope.ParentName];
                }

                // Initialize current and ancestor scopes
                while (scopesToInitialize?.Count > 0)
                {
                    scope = scopesToInitialize.Pop();
                    executionContext.Debug($"Initializing scope '{scope.Name}'");

                    // This is what matters, it stomps the current "steps" attribute with the parent's scope at first. 
                    executionContext.ExpressionValues["steps"] = stepsContext.GetScope(scope.ParentName);
                    if (!executionContext.ExpressionValues.ContainsKey("inputs"))
                    {
                        executionContext.ExpressionValues["inputs"] = !String.IsNullOrEmpty(scope.ParentName) ? scopeInputs[scope.ParentName] : null;
                    }
                    var templateEvaluator = executionContext.ToPipelineTemplateEvaluator();
                    var inputs = default(DictionaryContextData);
                    try
                    {
                        inputs = templateEvaluator.EvaluateStepScopeInputs(scope.Inputs, executionContext.ExpressionValues, executionContext.ExpressionFunctions);
                    }
                    catch (Exception ex)
                    {
                        Trace.Info($"Caught exception from initialize scope '{scope.Name}'");
                        Trace.Error(ex);
                        executionContext.Error(ex);
                        executionContext.Complete(TaskResult.Failed);
                    }

                    scopeInputs[scope.Name] = inputs;
                }
            }

            // Setup expression values
            var scopeName = executionContext.ScopeName;
            executionContext.ExpressionValues["steps"] = stepsContext.GetScope(scopeName);
            if (!executionContext.ExpressionValues.ContainsKey("inputs"))
            {
                executionContext.ExpressionValues["inputs"] = string.IsNullOrEmpty(scopeName) ? null : scopeInputs[scopeName];
            }
        }

        public Task RunAsync(ActionRunStage stage)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            var githubContext = ExecutionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));

            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);

            // Resolve action steps
            var actionSteps = Data.Steps;

            // Create Context Data to reuse for each composite action step
            var inputsData = new DictionaryContextData();
            foreach (var i in Inputs)
            {
                inputsData[i.Key] = new StringContextData(i.Value);
            }

            // Get Environment Data for Composite Action
            var extraExpressionValues = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
            extraExpressionValues["inputs"] = inputsData;
            var manifestManager = HostContext.GetService<IActionManifestManager>();

            // Add the composite action environment variables to each step.
            // If the key already exists, we override it since the composite action env variables will have higher precedence
            // Note that for each composite action step, it's environment variables will be set in the StepRunner automatically
            var compositeEnvData = manifestManager.EvaluateCompositeActionEnvironment(ExecutionContext, Data.Environment, extraExpressionValues);
            var envData = new Dictionary<string, string>();

            // Copy over parent environment
            foreach (var env in ExecutionContext.EnvironmentVariables)
            {
                envData[env.Key] = env.Value;
            }
            // Overwrite with current env
            foreach (var env in compositeEnvData)
            {
                envData[env.Key] = env.Value;
            }

            // Add each composite action step to the front of the queue
            int location = 0;
            foreach (Pipelines.ActionStep aStep in actionSteps)
            {
                // Ex: 
                // runs:
                //      using: "composite"
                //      steps:
                //          - uses: example/test-composite@v2 (a)
                //          - run echo hello world (b)
                //          - run echo hello world 2 (c)
                // 
                // ethanchewy/test-composite/action.yaml
                // runs:
                //      using: "composite"
                //      steps: 
                //          - run echo hello world 3 (d)
                //          - run echo hello world 4 (e)
                // 
                // Steps processed as follow:
                // | a |
                // | a | => | d |
                // (Run step d)
                // | a | 
                // | a | => | e |
                // (Run step e)
                // | a | 
                // (Run step a)
                // | b | 
                // (Run step b)
                // | c |
                // (Run step c)
                // Done.

                var actionRunner = HostContext.CreateService<IActionRunner>();
                actionRunner.Action = aStep;
                actionRunner.Stage = stage;
                actionRunner.Condition = aStep.Condition;
                actionRunner.DisplayName = aStep.DisplayName;
                var step = ExecutionContext.RegisterNestedStep(actionRunner, inputsData, location, envData);

                // Initialize Scope and Env Here
                // For reference, this used to be part of StepsRunner.cs but would only be used for Composite Actions.
                var scopeInputs = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
                InitializeScope(step, scopeInputs);

                location++;
            }

            // Gather outputs and clean up outputs in one step

            // Maybe we want to condense this into a function in ExecutionContext?
            // var postStepRunner = HostContext.CreateService<IActionRunner>();
            // postStepRunner.Action = new Pipelines.ActionStep();
            // postStepRunner.Stage = ActionRunStage.CompositePost;
            // postStepRunner.Condition = "always()";
            // postStepRunner.DisplayName = "Composite Post Step Cleanup";
            // ExecutionContext.RegisterNestedStep(postStepRunner, inputsData, location, envData);

            return Task.CompletedTask;
        }

    }
}
