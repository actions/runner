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

        public Task RunAsync(ActionRunStage stage)
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));

            var githubContext = ExecutionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));

            var tempDirectory = HostContext.GetDirectory(WellKnownDirectory.Temp);

            // Create Context Data to reuse for each composite action step
            var inputsData = new DictionaryContextData();
            foreach (var i in Inputs)
            {
                inputsData[i.Key] = new StringContextData(i.Value);
            }

            // Add each composite action step to the front of the queue
            int location = 0;

            // Resolve action steps
            var compositeSteps = Data.Steps;

            // TODO: Assume that each step is not an actionStep
            // How do we handle all types of steps?????

            // While loop till we have reached the last layer?
            List<Pipelines.Step> stepsToAppend = new List<Pipelines.Step>();

            // First put each step in stepsToAppend
            foreach (var step in compositeSteps)
            {
                stepsToAppend.Append(step);
            }

            // We go through each step and push to the top of the stack its children. 
            // That way, we go through each steps, steps of steps in order
            // This is an ITERATIVE approach. While a recursive approach may be more elegant, 
            // that would use a lot more memory in the call stack.
            // Ex: 
            // Let's say we have 4 composite steps with the first step that has 3 children
            // A (composite step)=> a1, a2, a3
            // B (non composite)
            // C (non composite)
            // D (non composite)
            // It would be executed in this order => a1, a2, a3, A (steps within A), B, C, D
            while (stepsToAppend != null)
            {
                var currentStep = stepsToAppend[0];

                // TODO: Create another StepsContext?
                // In the original StepsRunner, we could lock the thread and only proceed after we finish processing these steps
                // Then, we invoke the CompositeStepsRunner class?

                // TODO: We have to create another Execution Context for Composite Actions
                // See below

                // TODO: Append to StepsRunner
                // by invoking a RegisterNestedStep on the Composite Action Exeuction Context for Composite Action Steps


            }

            // foreach (Pipelines.Step aStep in actionSteps)
            // {
            //     // Ex: 
            //     // runs:
            //     //      using: "composite"
            //     //      steps:
            //     //          - uses: example/test-composite@v2 (a)
            //     //          - run echo hello world (b)
            //     //          - run echo hello world 2 (c)
            //     // 
            //     // ethanchewy/test-composite/action.yaml
            //     // runs:
            //     //      using: "composite"
            //     //      steps: 
            //     //          - run echo hello world 3 (d)
            //     //          - run echo hello world 4 (e)
            //     // 
            //     // Steps processed as follow:
            //     // | a |
            //     // | a | => | d |
            //     // (Run step d)
            //     // | a | 
            //     // | a | => | e |
            //     // (Run step e)
            //     // | a | 
            //     // (Run step a)
            //     // | b | 
            //     // (Run step b)
            //     // | c |
            //     // (Run step c)
            //     // Done.

            //     // TODO: how are we going to order each step?
            //     // How is this going to look in the UI (will we have a bunch of nesting)
            //     // ^ We need to focus on how we are going to get the steps to run in the right order. 

            //     var actionRunner = HostContext.CreateService<IActionRunner>();
            //     actionRunner.Action = aStep;
            //     actionRunner.Stage = stage;
            //     actionRunner.Condition = aStep.Condition;
            //     actionRunner.DisplayName = aStep.DisplayName;

            //     ExecutionContext.RegisterNestedStep(actionRunner, inputsData, location, Environment);
            //     location++;
            // }

            return Task.CompletedTask;
        }

    }
}
