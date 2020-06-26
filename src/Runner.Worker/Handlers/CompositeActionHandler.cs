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

            // Definition for the composite action step is located in ActionDefinition.GroupID

            Trace.Info($"Composite Action Scope Name: {ExecutionContext.ScopeName}");
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
                InitializeScope(step, scopeInputs);

                // We'll add the outputs in the scripthandler.cs instead

                location++;
                actionID++;
            }

            // Gather outputs and clean up outputs in one step

            // Basically we would do this in three steps:
            //      Gather all steps that have the same parentScopeID, 

            // We can attach the step of registered steps in the cleanOutputsStep!
            // or maybe we could just do an easy search across steps?
            // But they are popped from the step list: 

            Pipelines.ActionStep cleanOutputsStep = new Pipelines.ActionStep();
            cleanOutputsStep.StepID = actionID;
            cleanOutputsStep.GroupID = groupID;
            cleanOutputsStep.CleanUp = true;

            // Add pointers to outputs objects from each step since those steps will be removed from the JobSteps list
            // and won't be viewable for the cleanoutputsstep

            // How do we mangle all the outputs steps together from 
                //  handles it already
            // WE WILL GO THROUGH EACH STEP'S step.ExecutionContext: step["outputs"]


            // Maybe we want to condense this into a function in ExecutionContext?
            // var postStepRunner = HostContext.CreateService<IActionRunner>();
            // postStepRunner.Action = new Pipelines.ActionStep();
            // postStepRunner.Stage = ActionRunStage.CompositePost;
            // postStepRunner.Condition = "always()";
            // postStepRunner.DisplayName = "Composite Post Step Cleanup";
            // ExecutionContext.RegisterNestedStep(postStepRunner, inputsData, location, envData);


            // TODO: 6/25/20 => 6/26/20
            // We need to handle the Outputs Token in the general Action yaml file
            // We need to attach an "Outputs" attribute to each step. You can think of it like:
            // composite action : {
            //     step-1: {
            //         group-id: 
            //         id: 
            //         outputs: {
            //             ...
            //         }
            //     }
            //     step-2: {
            //         group-id: 
            //         id: 
            //         outputs: {
            //             ...
            //         }
            //     }
            // }
            // We need to add functionality to handle each output in each run step. See: SetOutputCommandExtension

            // Pop scope


            return Task.CompletedTask;
        }

    }
}
