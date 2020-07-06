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

        // private void InitializeScope(IStep step, Dictionary<string, PipelineContextData> scopeInputs)
        private void InitializeScope(IStep step)
        {
            var executionContext = step.ExecutionContext;
            var stepsContext = executionContext.StepsContext;
            if (!string.IsNullOrEmpty(executionContext.ScopeName))
            {
                // Gather uninitialized current and ancestor scopes
                Trace.Info($"Composite Actions Scopes: {StringUtil.ConvertToJson(executionContext.Scopes)}");
                // Add new scope if not created yet.
                if (!executionContext.Scopes.ContainsKey(executionContext.ScopeName))
                {
                    executionContext.Scopes[executionContext.ScopeName] = new Pipelines.ContextScope();
                    executionContext.Scopes[executionContext.ScopeName].Name = executionContext.ScopeName;
                }
                var scope = executionContext.Scopes[executionContext.ScopeName];
                
                executionContext.Debug($"Initializing scope '{scope.Name}'");

                step.ExecutionContext.ExpressionValues["steps"] = stepsContext.GetScope(scope.ParentName);

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
                    step.ExecutionContext.Error(ex);
                    step.ExecutionContext.Complete(TaskResult.Failed);
                }
            }

            // Setup expression values
            var scopeName = executionContext.ScopeName;
            step.ExecutionContext.ExpressionValues["steps"] = stepsContext.GetScope(scopeName);
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
            foreach (var env in Environment)
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

            Dictionary<string, string> scopesAndContexts = new Dictionary<string, string>();

            var parentScopeName = !String.IsNullOrEmpty(ExecutionContext.ScopeName) ? ExecutionContext.ScopeName : ExecutionContext.ContextName;
            Trace.Info($"Parent Scope Name {parentScopeName}");

            foreach (Pipelines.ActionStep aStep in actionSteps)
            {
                // Scope Names are not set for some reason for the Action nor for its steps.
                // TODO: figure out why
                // Trace.Info($"CompositeActionHandler ActionStep ScopeName {aStep.ScopeName}");
                Trace.Info($"CompositeActionHandler ActionStep Name {aStep.Name}");

                // TODO: We need to set the ScopeName to be able to evaluate the Outputs!!

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

                InitializeScope(step);

                location++;
            }

            

            // TODO: Figure out if we need to include workflow step ID for parentScopeName
            if (!ExecutionContext.Scopes.ContainsKey(parentScopeName)) {
                ExecutionContext.Scopes[parentScopeName] = new Pipelines.ContextScope() {
                    Name = parentScopeName
                };
            }

            // Create a step that handles all the composite action steps' outputs
            Pipelines.ActionStep cleanOutputsStep = new Pipelines.ActionStep();
            cleanOutputsStep.ContextName = ExecutionContext.ContextName;
            cleanOutputsStep.Reference = new Pipelines.CompositeOutputReference(
                scopeAndContextNames: null,
                parentScopeName: parentScopeName,
                outputs: Data.Outputs
            );
            var actionRunner2 = HostContext.CreateService<IActionRunner>();
            actionRunner2.Action = cleanOutputsStep;
            actionRunner2.Stage = ActionRunStage.Main;
            actionRunner2.Condition = "always()";
            actionRunner2.DisplayName = "Composite Action Steps Cleanup";
            ExecutionContext.RegisterNestedStep(actionRunner2, inputsData, location, envData, true);

            return Task.CompletedTask;
        }

    }
}
