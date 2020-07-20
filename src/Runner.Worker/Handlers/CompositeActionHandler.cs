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
            ArgUtil.NotNull(Data.Steps, nameof(Data.Steps));

            var githubContext = ExecutionContext.ExpressionValues["github"] as GitHubContext;
            ArgUtil.NotNull(githubContext, nameof(githubContext));

            // Resolve action steps
            var actionSteps = Data.Steps;

            // Create Context Data to reuse for each composite action step
            var inputsData = new DictionaryContextData();
            foreach (var i in Inputs)
            {
                inputsData[i.Key] = new StringContextData(i.Value);
            }

            // Initialize Composite Steps List of Steps
            var compositeSteps = new List<IStep>();

            // Temporary hack until after M271-ish. After M271-ish the server will never send an empty
            // context name. Generated context names start with "__"
            var childScopeName = ExecutionContext.GetFullyQualifiedContextName();
            if (string.IsNullOrEmpty(childScopeName))
            {
                childScopeName = $"__{Guid.NewGuid()}";
            }

            foreach (Pipelines.ActionStep actionStep in actionSteps)
            {
                var actionRunner = HostContext.CreateService<IActionRunner>();
                actionRunner.Action = actionStep;
                actionRunner.Stage = stage;
                actionRunner.Condition = actionStep.Condition;

                var step = ExecutionContext.CreateCompositeStep(childScopeName, actionRunner, inputsData, Environment);
                compositeSteps.Add(step);
            }

            try
            {
                // This is where we run each step.
                await RunStepsAsync(compositeSteps);

                // Get the pointer of the correct "steps" object and pass it to the ExecutionContext so that we can process the outputs correctly
                ExecutionContext.ExpressionValues["inputs"] = inputsData;
                ExecutionContext.ExpressionValues["steps"] = ExecutionContext.Global.StepsContext.GetScope(ExecutionContext.GetFullyQualifiedContextName());

                ProcessCompositeActionOutputs();
            }
            catch (Exception ex)
            {
                // Composite StepRunner should never throw exception out.
                Trace.Error($"Caught exception from composite steps {nameof(CompositeActionHandler)}: {ex}");
                ExecutionContext.Error(ex);
                ExecutionContext.Result = TaskResult.Failed;
            }
        }

        private void ProcessCompositeActionOutputs()
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

                // Get the evluated composite outputs' values mapped to the outputs named
                DictionaryContextData actionOutputs = actionManifestManager.EvaluateCompositeOutputs(ExecutionContext, Data.Outputs, evaluateContext);

                // Set the outputs for the outputs object in the whole composite action
                // Each pair is structured like this
                // We ignore "description" for now
                // {
                //   "the-output-name": {
                //     "description": "",
                //     "value": "the value"
                //   },
                //   ...
                // }
                foreach (var pair in actionOutputs)
                {
                    var outputsName = pair.Key;
                    var outputsAttributes = pair.Value as DictionaryContextData;
                    outputsAttributes.TryGetValue("value", out var val);
                    var outputsValue = val as StringContextData;

                    // Set output in the whole composite scope. 
                    if (!String.IsNullOrEmpty(outputsName) && !String.IsNullOrEmpty(outputsValue))
                    {
                        ExecutionContext.SetOutput(outputsName, outputsValue, out _);
                    }
                }
            }
        }

        private async Task RunStepsAsync(List<IStep> compositeSteps)
        {
            ArgUtil.NotNull(compositeSteps, nameof(compositeSteps));

            // The parent StepsRunner of the whole Composite Action Step handles the cancellation stuff already. 
            foreach (IStep step in compositeSteps)
            {
                System.Threading.Thread.Sleep(2000);

                var stepCancellationToken = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(ExecutionContext.CancellationToken);
                
                Trace.Info($"Processing composite step: DisplayName='{step.DisplayName}'");

                step.ExecutionContext.ExpressionValues["steps"] = ExecutionContext.Global.StepsContext.GetScope(step.ExecutionContext.ScopeName);

                // Populate env context for each step
                Trace.Info("Initialize Env context for step");
#if OS_WINDOWS
                var envContext = new DictionaryContextData();
#else
                var envContext = new CaseSensitiveDictionaryContextData();
#endif

                // Global env
                foreach (var pair in ExecutionContext.Global.EnvironmentVariables)
                {
                    envContext[pair.Key] = new StringContextData(pair.Value ?? string.Empty);
                }

                // Stomps over with outside step env
                if (step.ExecutionContext.ExpressionValues.TryGetValue("env", out var envContextData))
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

                step.ExecutionContext.ExpressionValues["env"] = envContext;

                var actionStep = step as IActionRunner;

                // Set GITHUB_ACTION
                step.ExecutionContext.SetGitHubContext("action", step.ExecutionContext.GetFullyQualifiedContextName());

                try
                {
                    // Evaluate and merge action's env block to env context
                    var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();
                    var actionEnvironment = templateEvaluator.EvaluateStepEnvironment(actionStep.Action.Environment, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions, Common.Util.VarUtil.EnvironmentVariableKeyComparer);
                    foreach (var env in actionEnvironment)
                    {
                        envContext[env.Key] = new StringContextData(env.Value ?? string.Empty);
                    }
                }
                catch (Exception ex)
                {
                    // fail the step since there is an evaluate error.
                    Trace.Info("Caught exception in Composite Steps Runner from expression for step.env");
                    // evaluateStepEnvFailed = true;
                    step.ExecutionContext.Error(ex);
                    step.ExecutionContext.Complete(TaskResult.Failed);
                }

                // Handle Cancellation
                // We will break out of loop immediately and display the result
                if (ExecutionContext.CancellationToken.IsCancellationRequested)
                {
                    ExecutionContext.Result = TaskResult.Canceled;
                    break;
                }
                else if (stepCancellationToken.IsCancellationRequested)
                {
                    ExecutionContext.Error("The action has timed out.");
                    ExecutionContext.Result = TaskResult.Failed;
                    break;
                }

                await RunStepAsync(step);

                // Handle Failed Step
                // We will break out of loop immediately and display the result
                if (step.ExecutionContext.Result == TaskResult.Failed)
                {
                    ExecutionContext.Result = step.ExecutionContext.Result;
                    break;
                }

                // TODO: Add compat for other types of steps.
            }
            // Completion Status handled by StepsRunner for the whole Composite Action Step
        }

        private async Task RunStepAsync(IStep step)
        {
            // Try to evaluate the display name
            if (step is IActionRunner actionRunner && actionRunner.Stage == ActionRunStage.Main)
            {
                actionRunner.TryEvaluateDisplayName(step.ExecutionContext.ExpressionValues, step.ExecutionContext);
            }

            // Start the step.
            Trace.Info("Starting the step.");
            step.ExecutionContext.Debug($"Starting: {step.DisplayName}");

            // TODO: Fix for Step Level Timeout Attributes for an individual Composite Run Step
            // For now, we are not going to support this for an individual composite run step

            var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();

            await Common.Util.EncodingUtil.SetEncoding(HostContext, Trace, step.ExecutionContext.CancellationToken);

            try
            {
                await step.RunAsync();
            }
            catch (OperationCanceledException ex)
            {
                Trace.Info("COMPOSITE OUTPUT CANCELLING LOOP");
                if (ExecutionContext.CancellationToken.IsCancellationRequested)
                {
                    Trace.Error($"Caught timeout exception from step: {ex.Message}");
                    step.ExecutionContext.Error("The action has timed out.");
                    step.ExecutionContext.Result = TaskResult.Failed;
                }
                else
                {
                    // Log the exception and cancel the step.
                    Trace.Error($"Caught cancellation exception from step: {ex}");
                    step.ExecutionContext.Error(ex);
                    step.ExecutionContext.Result = TaskResult.Canceled;
                }
            }
            catch (Exception ex)
            {
                // Log the error and fail the step.
                Trace.Error($"Caught exception from step: {ex}");
                step.ExecutionContext.Error(ex);
                step.ExecutionContext.Result = TaskResult.Failed;
            }

            // Merge execution context result with command result
            if (step.ExecutionContext.CommandResult != null)
            {
                step.ExecutionContext.Result = Common.Util.TaskResultUtil.MergeTaskResults(step.ExecutionContext.Result, step.ExecutionContext.CommandResult.Value);
            }

            // Fixup the step result if ContinueOnError.
            if (step.ExecutionContext.Result == TaskResult.Failed)
            {
                var continueOnError = false;
                try
                {
                    continueOnError = templateEvaluator.EvaluateStepContinueOnError(step.ContinueOnError, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions);
                }
                catch (Exception ex)
                {
                    Trace.Info("The step failed and an error occurred when attempting to determine whether to continue on error.");
                    Trace.Error(ex);
                    step.ExecutionContext.Error("The step failed and an error occurred when attempting to determine whether to continue on error.");
                    step.ExecutionContext.Error(ex);
                }

                if (continueOnError)
                {
                    step.ExecutionContext.Outcome = step.ExecutionContext.Result;
                    step.ExecutionContext.Result = TaskResult.Succeeded;
                    Trace.Info($"Updated step result (continue on error)");
                }
            }
            Trace.Info($"Step result: {step.ExecutionContext.Result}");

            // Complete the step context.
            step.ExecutionContext.Debug($"Finishing: {step.DisplayName}");
        }
    }
}
