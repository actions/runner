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

            // If clean up outputs step, we finish setting the outputs object and then return out of this function.
            if (ExecutionContext.FinalizeContext != null)
            {
                HandleOutput();
                return;
            }

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
                await RunAsync(ExecutionContext);
            }
            catch (Exception ex)
            {
                // Composite StepRunner should never throw exception out.
                Trace.Error($"Caught exception from composite steps {nameof(CompositeActionHandler)}: {ex}");
                ExecutionContext.Error(ex);
                ExecutionContext.Result = TaskResult.Failed;
            }
        }
        public async Task RunAsync(IExecutionContext actionContext)
        {
            // Another approach we can explore, is moving all this logic to the CompositeActionHandler if it's small enough. 

            ArgUtil.NotNull(actionContext, nameof(actionContext));
            ArgUtil.NotNull(actionContext.CompositeSteps, nameof(actionContext.CompositeSteps));

            // The parent StepsRunner of the whole Composite Action Step handles the cancellation stuff already. 
            foreach (IStep step in actionContext.CompositeSteps)
            {
                // This is used for testing UI appearance.
                // System.Threading.Thread.Sleep(5000);

                Trace.Info($"Processing composite step: DisplayName='{step.DisplayName}'");

                step.ExecutionContext.ExpressionValues["steps"] = step.ExecutionContext.StepsContext.GetScope(step.ExecutionContext.ScopeName);

                // Populate env context for each step
                Trace.Info("Initialize Env context for step");
#if OS_WINDOWS
                var envContext = new DictionaryContextData();
#else
                var envContext = new CaseSensitiveDictionaryContextData();
#endif

                // Global env
                foreach (var pair in step.ExecutionContext.EnvironmentVariables)
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
                if (!String.IsNullOrEmpty(step.ExecutionContext.ScopeName))
                {
                    step.ExecutionContext.SetGitHubContext("action", step.ExecutionContext.ScopeName);
                }
                else
                {
                    step.ExecutionContext.SetGitHubContext("action", step.ExecutionContext.ContextName);
                }

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
                    CompleteStep(step, TaskResult.Failed);
                }

                // We don't have to worry about the cancellation token stuff because that's handled by the composite action level (in the StepsRunner)

                await RunStepAsync(step);

                // TODO: Add compat for other types of steps.
            }
            // Completion Status handled by StepsRunner for the whole Composite Action Step
        }

        private void CompleteStep(IStep step, TaskResult? result = null, string resultCode = null)
        {
            var executionContext = step.ExecutionContext;

            executionContext.Complete(result, resultCode: resultCode);
        }

        private void HandleOutput()
        {
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
                actionManifestManager.SetAllCompositeOutputs(ExecutionContext.FinalizeContext, actionOutputs);
            }
        }

        private void InitializeScope(IStep step)
        {
            var stepsContext = step.ExecutionContext.StepsContext;
            var scopeName = step.ExecutionContext.ScopeName;
            step.ExecutionContext.ExpressionValues["steps"] = stepsContext.GetScope(scopeName);
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

            // Set the timeout
            var timeoutMinutes = 0;
            var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();
            try
            {
                timeoutMinutes = templateEvaluator.EvaluateStepTimeout(step.Timeout, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions);
            }
            catch (Exception ex)
            {
                Trace.Info("An error occurred when attempting to determine the step timeout.");
                Trace.Error(ex);
                step.ExecutionContext.Error("An error occurred when attempting to determine the step timeout.");
                step.ExecutionContext.Error(ex);
            }
            if (timeoutMinutes > 0)
            {
                var timeout = TimeSpan.FromMinutes(timeoutMinutes);
                step.ExecutionContext.SetTimeout(timeout);
            }

            Common.Util.EncodingUtil.EnsureUtfEncoding(HostContext, Trace, step.ExecutionContext.CancellationToken);

            try
            {
                await step.RunAsync();
            }
            catch (OperationCanceledException ex)
            {
                if (step.ExecutionContext.CancellationToken.IsCancellationRequested)
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
