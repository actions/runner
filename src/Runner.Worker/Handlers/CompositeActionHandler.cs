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

        public async Task RunAsync(ActionRunStage stage)
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

            // Add each composite action step to the front of the queue
            int location = 0;

            // Initialize Steps Runner for Composite Steps and Environment for it
            var compositeStepsRunner = HostContext.CreateService<ICompositeStepsRunner>();

            // Initialize Composite Steps List of Steps
            ExecutionContext.CompositeSteps = new List<IStep>();

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

                var step = ExecutionContext.RegisterNestedStep(actionRunner, inputsData, location, Environment);
                InitializeScope(step);

                // Add all steps to the Composite StepsRunner
                // Follows similar logic to how JobRunner invokes the StepsRunner for job steps!
                ExecutionContext.CompositeSteps.Add(step);

                location++;
            }

            // Create a step that handles all the composite action steps' outputs
            Pipelines.ActionStep cleanOutputsStep = new Pipelines.ActionStep();
            cleanOutputsStep.ContextName = ExecutionContext.ContextName;
            // Use the same reference type as our composite steps.
            cleanOutputsStep.Reference = Action;

            var actionRunner2 = HostContext.CreateService<IActionRunner>();
            actionRunner2.Action = cleanOutputsStep;
            actionRunner2.Stage = ActionRunStage.Main;
            actionRunner2.Condition = "always()";
            var cleanUpStep = ExecutionContext.RegisterNestedStep(actionRunner2, inputsData, location, Environment, true);
            ExecutionContext.CompositeSteps.Add(cleanUpStep);

            // Then run the Composite StepsRunner
            try 
            {
                await compositeStepsRunner.RunAsync(ExecutionContext);
            }
            catch (Exception ex)
            {
                // Composite StepRunner should never throw exception out.
                Trace.Error($"Caught exception from composite steps {nameof(CompositeStepsRunner)}: {ex}");
                ExecutionContext.Error(ex);
                ExecutionContext.Result = TaskResult.Failed;
            }
            ExecutionContext.Result = TaskResult.Succeeded;
        }

        private void InitializeScope(IStep step)
        {
            var stepsContext = step.ExecutionContext.StepsContext;
            var scopeName = step.ExecutionContext.ScopeName;
            step.ExecutionContext.ExpressionValues["steps"] = stepsContext.GetScope(scopeName);
        }
    }
}
