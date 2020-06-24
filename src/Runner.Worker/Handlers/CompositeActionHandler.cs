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
                // Maybe we have to change the type of Step to Composite Step
                // and create a new type of Step
                // We have to keep track of the collection of the composite steps as a whole
                // Like the composite steps should have the same ID
                // Example attributes for a composite step instance:
                /*
                {
                    composite_id: ~string defined in the workflow file?~,
                    step_number: ~int~,

                }
                */
                // ^ Benefits of this approach is: 
                // 1) We can easily condense these steps into one node retroactively or actively.
                // 2) Easily aggregate the outputs together.

                // Or maybe we would use a different handler type?
                // Like we could have CompositeActionOutputHandler
                // and ActionExecutionType.CompositeActionOutput
                // But how would hte handler differentiate between steps from different composite actions?
                // Maybe add an attribute to an ExecutionContext.
                actionRunner.Action = aStep;
                actionRunner.Stage = stage;
                actionRunner.Condition = aStep.Condition;
                actionRunner.DisplayName = aStep.DisplayName;

                ExecutionContext.RegisterNestedStep(actionRunner, inputsData, location, envData);
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
