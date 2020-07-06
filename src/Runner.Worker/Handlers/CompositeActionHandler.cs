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
                // var scopesToInitialize = default(Stack<Pipelines.ContextScope>);
                // while (scope != null && !scopeInputs.ContainsKey(scope.Name))
                // {
                //     if (scopesToInitialize == null)
                //     {
                //         scopesToInitialize = new Stack<Pipelines.ContextScope>();
                //     }
                //     scopesToInitialize.Push(scope);
                //     Trace.Info($"Scopes: {StringUtil.ConvertToJson(executionContext.Scopes)}");
                //     scope = string.IsNullOrEmpty(scope.ParentName) ? null : executionContext.Scopes[scope.ParentName];
                // }


                // Initialize current and ancestor scopes
                // while (scopesToInitialize?.Count > 0)
                // {
                //     scope = scopesToInitialize.Pop();
                //     executionContext.Debug($"Initializing scope '{scope.Name}'");

                //     // This is what matters, it stomps the current "steps" attribute with the parent's scope at first. 
                    // executionContext.ExpressionValues["steps"] = stepsContext.GetScope(scope.ParentName);
                //     if (!executionContext.ExpressionValues.ContainsKey("inputs"))
                //     {
                //         executionContext.ExpressionValues["inputs"] = !String.IsNullOrEmpty(scope.ParentName) ? scopeInputs[scope.ParentName] : null;
                //     }
                //     var templateEvaluator = executionContext.ToPipelineTemplateEvaluator();
                //     var inputs = default(DictionaryContextData);
                //     try
                //     {
                //         inputs = templateEvaluator.EvaluateStepScopeInputs(scope.Inputs, executionContext.ExpressionValues, executionContext.ExpressionFunctions);
                //     }
                //     catch (Exception ex)
                //     {
                //         Trace.Info($"Caught exception from initialize scope '{scope.Name}'");
                //         Trace.Error(ex);
                //         executionContext.Error(ex);
                //         executionContext.Complete(TaskResult.Failed);
                //     }

                //     scopeInputs[scope.Name] = inputs;
                // }
                executionContext.Debug($"Initializing scope '{scope.Name}'");

                // This is what matters, it stomps the current "steps" attribute with the parent's scope at first. 
                step.ExecutionContext.ExpressionValues["steps"] = stepsContext.GetScope(scope.ParentName);
                // if (!step.ExecutionContext.ExpressionValues.ContainsKey("inputs"))
                // {
                //     step.ExecutionContext.ExpressionValues["inputs"] = !String.IsNullOrEmpty(scope.ParentName) ? scopeInputs[scope.ParentName] : null;
                // }
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

                // scopeInputs[scope.Name] = inputs;
            }

            // Setup expression values
            var scopeName = executionContext.ScopeName;
            step.ExecutionContext.ExpressionValues["steps"] = stepsContext.GetScope(scopeName);
            // if (!executionContext.ExpressionValues.ContainsKey("inputs"))
            // {
            //     executionContext.ExpressionValues["inputs"] = string.IsNullOrEmpty(scopeName) ? null : scopeInputs[scopeName];
            // }
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

            // ID for Composite Action Step
            // You can easily identify it by doing ScopeName.<actionID> to get the specific step.
            // Eventually we would want to assign IDs to job steps as well?
            // We have to make sure that every job step is unique from each other
            // We have to also make sure that every composite action step is unique from all other steps within its parent
            int actionID = 0;

            // File path has to be unique so we can just generate a UUID for the group of composite steps from that?
            int groupID = Data.StepsGroupID;

            Trace.Info($"Composite Action GroupID: {groupID}");

            // Definition for the composite action step is located in ActionDefinition.GroupID
            // TODO: Scope Name not set for some reason
            // I think only the scopes of the steps of the action are set for some reason.s
            // By default, let's set this to the filename just in case "name" is not set by user.

            // We can just access the ContextName instead
            Trace.Info($"Composite Action Context Name: {ExecutionContext.ContextName}");

            // Nullified Outputs
            // TODO: Pass these output key values to be set in the Job StepContext's Outputs variables. 
            // TODO: How will the workflow file know how to access the StepContext's Outputs variables?

            // TODO: Evaluate Outputs
            // TODO: Add function for evaluating Outputs token.
            // if (Data.Outputs != null) {
            //     foreach (var pair in Data.Outputs) {
            //         Trace.Info($"Composite Action Handler. Original Output Key: {pair.Key}");
            //         Trace.Info($"Composite Action Handler. Original Output Value: {pair.Value}");
            //         Data.Outputs[pair.Key] = null;
            //     }
            // }

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

                // Trace.Info($"CompositeAction Handler Step Number {aStep.Id}");

                // There is an ID already assigned to each Step (See: PipelineTemplateConverter::ConvertToStep)
                // In the future we want the GUID + Step ID to be the same.

                // TODO: CONSIDER DOING THIS IN ConvertStep which is called by LoadCompositeSteps in PipelineTemplateEvaluator.
                aStep.StepID = actionID;
                aStep.GroupID = groupID;

                // TODO: How do we create an ID to connect all steps within a composite action?
                // We can create an GroupID to easily identify the scope of the step. 
                // ^ should this be in the ExecutionContext?

                var actionRunner = HostContext.CreateService<IActionRunner>();
                actionRunner.Action = aStep;
                actionRunner.Stage = stage;
                actionRunner.Condition = aStep.Condition;
                actionRunner.DisplayName = aStep.DisplayName;

                // TODO: pass in the name of the current action step
                var step = ExecutionContext.RegisterNestedStep(actionRunner, inputsData, location, envData);

                // How do we identify the ID of the parent? => Auto set when Runner is start?
                // Lol, should we attach it to the token
                // In ExecutionContext?

                // TODO: Add Step IDs in a systemic fashion. 
                // TODO: Add Scope ID in a systemic fashion. 
                // ^ We are doing the above to easily resolve scopes in the future!
                // Might be best to do this in LoadSteps!!
                // ^ in the executionContext? or the ActionStep?
                // ^ don't put it in template token that makes no sense.

                // Initialize Scope and Env Here
                // For reference, this used to be part of StepsRunner.cs but would only be used for Composite Actions.
                var scopeInputs = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
                // InitializeScope(step, scopeInputs);
                InitializeScope(step);

                // TODO: Ensure that the composite run step always has an ID. 
                // If they didn't set an ID, then generate an ID and mark it as a "__asdf" => then in further code, don't process. 
                // If scope name is "__"
                Trace.Info("Scope Name: {step.ExecutionContext.ScopeName}");
                // if (!String.IsNullOrEmpty(step.ExecutionContext.ScopeName)) {
                //     scopesAndContexts.Add(step.ExecutionContext.ScopeName, step.ExecutionContext.ContextName);
                // }

                location++;
                actionID++;
            }

            

            // TODO: Figure out if we need to include workflow step ID for parentScopeName
            if (!ExecutionContext.Scopes.ContainsKey(parentScopeName)) {
                ExecutionContext.Scopes[parentScopeName] = new Pipelines.ContextScope() {
                    Name = parentScopeName
                };
            }

            // Gather outputs and clean up outputs in one step

            // Basically we would do this in three steps:
            //      Gather all steps that have the same parentScopeID, 

            // We can attach the step of registered steps in the cleanOutputsStep!
            // or maybe we could just do an easy search across steps?
            // But they are popped from the step list: 

            // TODO: Make the composite step have the same scope name as the nested steps. 

            Pipelines.ActionStep cleanOutputsStep = new Pipelines.ActionStep();
            cleanOutputsStep.StepID = actionID;
            cleanOutputsStep.GroupID = groupID;
            cleanOutputsStep.CleanUp = true;
            cleanOutputsStep.ContextName = ExecutionContext.ContextName;
            // Go ask ting about implementation
            
            // TODO: Figure out a way to do this without Reference Type.
            // Maybe put it on the Execution Context. 
            cleanOutputsStep.Reference = new Pipelines.CompositeOutputReference(
                scopeAndContextNames: null,
                parentScopeName: parentScopeName,
                outputs: Data.Outputs
            );

            // Try changing data.ExecutionType to ActionSource

            // TODO: Pass parents stuff.
            // We have access to this already via the step.Environment variable. 
            // cleanOutputsStep.ScopeName = ExecutionContext.ContextName;
            // Wait can't we just use the same executionContext as the parent for now?

            var actionRunner2 = HostContext.CreateService<IActionRunner>();
            actionRunner2.Action = cleanOutputsStep;
            // actionRunner2.Stage = ActionRunStage.CompositePost;
            actionRunner2.Condition = "always()";
            actionRunner2.DisplayName = "Composite Action Steps Cleanup";

            // Figure out how to pass the steps stuff through. 

            ExecutionContext.RegisterNestedStep(actionRunner2, inputsData, location, envData, true);

            return Task.CompletedTask;
        }

    }
}
