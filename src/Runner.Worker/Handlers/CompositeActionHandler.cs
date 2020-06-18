using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System;
using System.Linq;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using System.Collections.Generic;
using GitHub.DistributedTask.Pipelines.ContextData;

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
            if (actionSteps == null)
            {
                Trace.Error("Data.Steps in CompositeActionHandler is null");
            }
            else
            {
                Trace.Info($"Data Steps Value for Composite Actions is: {actionSteps}.");
            }

            // Create Context Data to reuse for each composite action step
            var inputsData = new DictionaryContextData();
            foreach (var i in Inputs)
            {
                inputsData[i.Key] = new StringContextData(i.Value);
            }

            // Set up parent's environment data and then add on composite action environment data
#if OS_WINDOWS
            var envData = ExecutionContext.ExpressionValues["env"].Clone() as DictionaryContextData;
#else
            var envData = ExecutionContext.ExpressionValues["env"].Clone() as CaseSensitiveDictionaryContextData;
#endif
            // Composite action will have already inherited the root env attributes. 
            // We evaluated the env simimilar to how ContainerActionHandler does it.
            if (Data.Environment == null) {
                Trace.Info($"Composite Env Mapping Token is null");
            } else {
                Trace.Info($"Composite Env Mapping Token {Data.Environment}");
            }
            var extraExpressionValues = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
            extraExpressionValues["inputs"] = inputsData;
            var manifestManager = HostContext.GetService<IActionManifestManager>();
            var evaluatedEnv = manifestManager.EvaluateCompositeActionEnvironment(ExecutionContext, Data.Environment, extraExpressionValues);
            foreach (var e in evaluatedEnv)
            {
                // How to add to EnvironmentContextData
                // We need to use IEnvironmentContextData because ScriptHandler uses this type for environment variables
                Trace.Info($"Composite Action Env Key: {e.Key}");
                Trace.Info($"Composite Action Env Value: {e.Value}");
                envData[e.Key] = new StringContextData(e.Value);
            }

            // Add each composite action step to the front of the queue
            var compositeActionSteps = new Queue<IStep>();
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
                // Stack (LIFO) [Bottom => Middle => Top]:
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
                // TODO: Do we need to add any context data from the job message?
                // (See JobExtension.cs ~line 236)

                compositeActionSteps.Enqueue(ExecutionContext.RegisterCompositeStep(actionRunner, inputsData, envData));
            }
            ExecutionContext.EnqueueAllCompositeSteps(compositeActionSteps);

            return Task.CompletedTask;
        }

    }
}
