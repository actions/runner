using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            // Validate args
            Trace.Entering();
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));
            ArgUtil.NotNull(Inputs, nameof(Inputs));
            ArgUtil.NotNull(Data.Steps, nameof(Data.Steps));

            try
            {
                // Inputs of the composite step
                var inputsData = new DictionaryContextData();
                foreach (var i in Inputs)
                {
                    inputsData[i.Key] = new StringContextData(i.Value);
                }

                // Temporary hack until after M271-ish. After M271-ish the server will never send an empty
                // context name. Generated context names start with "__"
                var childScopeName = ExecutionContext.GetFullyQualifiedContextName();
                if (string.IsNullOrEmpty(childScopeName))
                {
                    childScopeName = $"__{Guid.NewGuid()}";
                }

                // Create embedded steps
                var embeddedSteps = new List<IStep>();
                foreach (Pipelines.ActionStep stepData in Data.Steps)
                {
                    var step = HostContext.CreateService<IActionRunner>();
                    step.Action = stepData;
                    step.Stage = stage;
                    step.Condition = stepData.Condition;
                    step.ExecutionContext = ExecutionContext.CreateEmbeddedChild(childScopeName, stepData.ContextName);
                    step.ExecutionContext.ExpressionValues["inputs"] = inputsData;
                    step.ExecutionContext.ExpressionValues["steps"] = ExecutionContext.Global.StepsContext.GetScope(childScopeName);

                    // Shallow copy github context
                    var gitHubContext = step.ExecutionContext.ExpressionValues["github"] as GitHubContext;
                    ArgUtil.NotNull(gitHubContext, nameof(gitHubContext));
                    gitHubContext = gitHubContext.ShallowCopy();
                    step.ExecutionContext.ExpressionValues["github"] = gitHubContext;

                    // Set GITHUB_ACTION_PATH
                    step.ExecutionContext.SetGitHubContext("action_path", ActionDirectory);

                    embeddedSteps.Add(step);
                }

                // Run embedded steps
                await RunStepsAsync(embeddedSteps);

                // Set outputs
                ExecutionContext.ExpressionValues["inputs"] = inputsData;
                ExecutionContext.ExpressionValues["steps"] = ExecutionContext.Global.StepsContext.GetScope(childScopeName);
                ProcessOutputs();
                ExecutionContext.Global.StepsContext.ClearScope(childScopeName);
            }
            catch (Exception ex)
            {
                // Composite StepRunner should never throw exception out.
                Trace.Error($"Caught exception from composite steps {nameof(CompositeActionHandler)}: {ex}");
                ExecutionContext.Error(ex);
                ExecutionContext.Result = TaskResult.Failed;
            }
        }

        private void ProcessOutputs()
        {
            ArgUtil.NotNull(ExecutionContext, nameof(ExecutionContext));

            // Evaluate the mapped outputs value
            if (Data.Outputs != null)
            {
                // Evaluate the outputs in the steps context to easily retrieve the values
                var actionManifestManager = HostContext.GetService<IActionManifestManager>();

                // Format ExpressionValues to Dictionary<string, PipelineContextData>
                var evaluateContext = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in ExecutionContext.ExpressionValues)
                {
                    evaluateContext[pair.Key] = pair.Value;
                }

                // Evaluate outputs
                DictionaryContextData actionOutputs = actionManifestManager.EvaluateCompositeOutputs(ExecutionContext, Data.Outputs, evaluateContext);

                // Set outputs
                //
                // Each pair is structured like:
                //   {
                //     "the-output-name": {
                //       "description": "",
                //       "value": "the value"
                //     },
                //     ...
                //   }
                foreach (var pair in actionOutputs)
                {
                    var outputName = pair.Key;
                    var outputDefinition = pair.Value as DictionaryContextData;
                    if (outputDefinition.TryGetValue("value", out var val))
                    {
                        var outputValue = val.AssertString("output value");
                        ExecutionContext.SetOutput(outputName, outputValue.Value, out _);
                    }
                }
            }
        }

        private async Task RunStepsAsync(List<IStep> embeddedSteps)
        {
            ArgUtil.NotNull(embeddedSteps, nameof(embeddedSteps));

            foreach (IStep step in embeddedSteps)
            {
                Trace.Info($"Processing embedded step: DisplayName='{step.DisplayName}'");

                // Initialize env context
                Trace.Info("Initialize Env context for embedded step");
#if OS_WINDOWS
                var envContext = new DictionaryContextData();
#else
                var envContext = new CaseSensitiveDictionaryContextData();
#endif
                step.ExecutionContext.ExpressionValues["env"] = envContext;

                // Merge global env
                foreach (var pair in ExecutionContext.Global.EnvironmentVariables)
                {
                    envContext[pair.Key] = new StringContextData(pair.Value ?? string.Empty);
                }

                // Merge composite-step env
                if (ExecutionContext.ExpressionValues.TryGetValue("env", out var envContextData))
                {
#if OS_WINDOWS
                    var dict = envContextData as DictionaryContextData;
#else
                    var dict = envContextData as CaseSensitiveDictionaryContextData;
#endif
                    foreach (var pair in dict)
                    {
                        envContext[pair.Key] = pair.Value;
                    }
                }

                var actionStep = step as IActionRunner;

                try
                {
                    // Evaluate and merge embedded-step env
                    var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();
                    var actionEnvironment = templateEvaluator.EvaluateStepEnvironment(actionStep.Action.Environment, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions, Common.Util.VarUtil.EnvironmentVariableKeyComparer);
                    foreach (var env in actionEnvironment)
                    {
                        envContext[env.Key] = new StringContextData(env.Value ?? string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    // Evaluation error
                    Trace.Info("Caught exception from expression for embedded step.env");
                    step.ExecutionContext.Error(ex);
                    step.ExecutionContext.Complete(TaskResult.Failed);
                }

                await RunStepAsync(step);

                // Check failed or canceled
                if (step.ExecutionContext.Result == TaskResult.Failed || step.ExecutionContext.Result == TaskResult.Canceled)
                {
                    ExecutionContext.Result = step.ExecutionContext.Result;
                    break;
                }
            }
        }

        private async Task RunStepAsync(IStep step)
        {
            Trace.Info($"Starting: {step.DisplayName}");
            step.ExecutionContext.Debug($"Starting: {step.DisplayName}");

            await Common.Util.EncodingUtil.SetEncoding(HostContext, Trace, step.ExecutionContext.CancellationToken);

            try
            {
                await step.RunAsync();
            }
            catch (OperationCanceledException ex)
            {
                if (step.ExecutionContext.CancellationToken.IsCancellationRequested &&
                    !ExecutionContext.Root.CancellationToken.IsCancellationRequested)
                {
                    Trace.Error($"Caught timeout exception from step: {ex.Message}");
                    step.ExecutionContext.Error("The action has timed out.");
                    step.ExecutionContext.Result = TaskResult.Failed;
                }
                else
                {
                    Trace.Error($"Caught cancellation exception from step: {ex}");
                    step.ExecutionContext.Error(ex);
                    step.ExecutionContext.Result = TaskResult.Canceled;
                }
            }
            catch (Exception ex)
            {
                // Log the error and fail the step
                Trace.Error($"Caught exception from step: {ex}");
                step.ExecutionContext.Error(ex);
                step.ExecutionContext.Result = TaskResult.Failed;
            }

            // Merge execution context result with command result
            if (step.ExecutionContext.CommandResult != null)
            {
                step.ExecutionContext.Result = Common.Util.TaskResultUtil.MergeTaskResults(step.ExecutionContext.Result, step.ExecutionContext.CommandResult.Value);
            }

            Trace.Info($"Step result: {step.ExecutionContext.Result}");
            step.ExecutionContext.Debug($"Finished: {step.DisplayName}");
        }
    }
}
