using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker;
using GitHub.Runner.Worker.Expressions;
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

            List<Pipelines.ActionStep> steps;

            if (stage == ActionRunStage.Pre)
            {
                ArgUtil.NotNull(Data.PreSteps, nameof(Data.PreSteps));
                steps = Data.PreSteps;
            }
            else if (stage == ActionRunStage.Post)
            {
                ArgUtil.NotNull(Data.PostSteps, nameof(Data.PostSteps));
                steps = new List<Pipelines.ActionStep>();
                // Only register post steps for steps that actually ran
                foreach (var step in Data.PostSteps.ToList())
                {
                    if (ExecutionContext.Root.EmbeddedStepsWithPostRegistered.ContainsKey(step.Id))
                    {
                        step.Condition = ExecutionContext.Root.EmbeddedStepsWithPostRegistered[step.Id];
                        steps.Add(step);
                    }
                    else
                    {
                        Trace.Info($"Skipping executing post step id: {step.Id}, name: ${step.DisplayName}");
                    }
                }
            }
            else
            {
                ArgUtil.NotNull(Data.Steps, nameof(Data.Steps));
                steps = Data.Steps;
            }

            // Set extra telemetry base on the current context.
            if (stage == ActionRunStage.Main)
            {
                var hasRunsStep = false;
                var hasUsesStep = false;
                foreach (var step in steps)
                {
                    if (step.Reference.Type == Pipelines.ActionSourceType.Script)
                    {
                        hasRunsStep = true;
                    }
                    else
                    {
                        hasUsesStep = true;
                    }
                }

                ExecutionContext.StepTelemetry.HasPreStep = Data.HasPre;
                ExecutionContext.StepTelemetry.HasPostStep = Data.HasPost;
                
                ExecutionContext.StepTelemetry.HasRunsStep = hasRunsStep;
                ExecutionContext.StepTelemetry.HasUsesStep = hasUsesStep;
                ExecutionContext.StepTelemetry.StepCount = steps.Count;
            }
            ExecutionContext.StepTelemetry.Type = "composite";

            try
            {
                // Inputs of the composite step
                var inputsData = new DictionaryContextData();
                foreach (var i in Inputs)
                {
                    inputsData[i.Key] = new StringContextData(i.Value);
                }

                // Temporary hack until after 3.2. After 3.2 the server will never send an empty
                // context name. Generated context names start with "__"
                var childScopeName = ExecutionContext.GetFullyQualifiedContextName();
                if (string.IsNullOrEmpty(childScopeName))
                {
                    childScopeName = $"__{Guid.NewGuid()}";
                }

                // Create embedded steps
                var embeddedSteps = new List<IStep>();

                // If we need to setup containers beforehand, do it
                // only relevant for local composite actions that need to JIT download/setup containers
                if (LocalActionContainerSetupSteps != null && LocalActionContainerSetupSteps.Count > 0)
                {
                    foreach (var step in LocalActionContainerSetupSteps)
                    {
                        ArgUtil.NotNull(step, step.DisplayName);
                        var stepId = $"__{Guid.NewGuid()}";
                        step.ExecutionContext = ExecutionContext.CreateEmbeddedChild(childScopeName, stepId, Guid.NewGuid(), stage);
                        embeddedSteps.Add(step);
                    }
                }
                foreach (Pipelines.ActionStep stepData in steps)
                {
                    // Compute child sibling scope names for post steps
                    // We need to use the main's scope to keep step context correct, makes inputs flow correctly
                    string siblingScopeName = null;
                    if (!String.IsNullOrEmpty(ExecutionContext.SiblingScopeName) && stage == ActionRunStage.Post)
                    {
                        siblingScopeName = $"{ExecutionContext.SiblingScopeName}.{stepData.ContextName}";
                    }

                    var step = HostContext.CreateService<IActionRunner>();
                    step.Action = stepData;
                    step.Stage = stage;
                    step.Condition = stepData.Condition;
                    ExecutionContext.Root.EmbeddedIntraActionState.TryGetValue(step.Action.Id, out var intraActionState);
                    step.ExecutionContext = ExecutionContext.CreateEmbeddedChild(childScopeName, stepData.ContextName, step.Action.Id, stage, intraActionState: intraActionState, siblingScopeName: siblingScopeName);
                    step.ExecutionContext.ExpressionValues["inputs"] = inputsData;
                    if (!String.IsNullOrEmpty(ExecutionContext.SiblingScopeName))
                    {
                        step.ExecutionContext.ExpressionValues["steps"] = ExecutionContext.Global.StepsContext.GetScope(ExecutionContext.SiblingScopeName);
                    }
                    else
                    {
                        step.ExecutionContext.ExpressionValues["steps"] = ExecutionContext.Global.StepsContext.GetScope(childScopeName);
                    }

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
                await RunStepsAsync(embeddedSteps, stage);

                // Set outputs
                ExecutionContext.ExpressionValues["inputs"] = inputsData;
                ExecutionContext.ExpressionValues["steps"] = ExecutionContext.Global.StepsContext.GetScope(childScopeName);
                ProcessOutputs();
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

        private async Task RunStepsAsync(List<IStep> embeddedSteps, ActionRunStage stage)
        {
            ArgUtil.NotNull(embeddedSteps, nameof(embeddedSteps));

            foreach (IStep step in embeddedSteps)
            {
                Trace.Info($"Processing embedded step: DisplayName='{step.DisplayName}'");

                // Add Expression Functions
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<HashFilesFunction>(PipelineTemplateConstants.HashFiles, 1, byte.MaxValue));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0));

                // Set action_status to the success of the current composite action
                var actionResult = ExecutionContext.Result?.ToActionResult() ?? ActionResult.Success;
                step.ExecutionContext.SetGitHubContext("action_status", actionResult.ToString());

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

                try
                {
                    if (step is IActionRunner actionStep)
                    {
                        // Evaluate and merge embedded-step env
                        var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();
                        var actionEnvironment = templateEvaluator.EvaluateStepEnvironment(actionStep.Action.Environment, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions, Common.Util.VarUtil.EnvironmentVariableKeyComparer);
                        foreach (var env in actionEnvironment)
                        {
                            envContext[env.Key] = new StringContextData(env.Value ?? string.Empty);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Evaluation error
                    Trace.Info("Caught exception from expression for embedded step.env");
                    step.ExecutionContext.Error(ex);
                    step.ExecutionContext.Complete(TaskResult.Failed);
                }

                // Register Callback
                CancellationTokenRegistration? jobCancelRegister = null;
                try
                {
                    // Register job cancellation call back only if job cancellation token not been fire before each step run
                    if (!ExecutionContext.Root.CancellationToken.IsCancellationRequested)
                    {
                        // Test the condition again. The job was cancelled after the condition was originally evaluated.
                        jobCancelRegister = ExecutionContext.Root.CancellationToken.Register(() =>
                        {
                            // Mark job as cancelled
                            ExecutionContext.Root.Result = TaskResult.Canceled;
                            ExecutionContext.Root.JobContext.Status = ExecutionContext.Root.Result?.ToActionResult();

                            step.ExecutionContext.Debug($"Re-evaluate condition on job cancellation for step: '{step.DisplayName}'.");
                            var conditionReTestTraceWriter = new ConditionTraceWriter(Trace, null); // host tracing only
                            var conditionReTestResult = false;
                            if (HostContext.RunnerShutdownToken.IsCancellationRequested)
                            {
                                step.ExecutionContext.Debug($"Skip Re-evaluate condition on runner shutdown.");
                            }
                            else
                            {
                                try
                                {
                                    var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator(conditionReTestTraceWriter);
                                    var condition = new BasicExpressionToken(null, null, null, step.Condition);
                                    conditionReTestResult = templateEvaluator.EvaluateStepIf(condition, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions, step.ExecutionContext.ToExpressionState());
                                }
                                catch (Exception ex)
                                {
                                    // Cancel the step since we get exception while re-evaluate step condition
                                    Trace.Info("Caught exception from expression when re-test condition on job cancellation.");
                                    step.ExecutionContext.Error(ex);
                                }
                            }

                            if (!conditionReTestResult)
                            {
                                // Cancel the step
                                Trace.Info("Cancel current running step.");
                                step.ExecutionContext.CancelToken();
                            }
                        });
                    }
                    else
                    {
                        if (ExecutionContext.Root.Result != TaskResult.Canceled)
                        {
                            // Mark job as cancelled
                            ExecutionContext.Root.Result = TaskResult.Canceled;
                            ExecutionContext.Root.JobContext.Status = ExecutionContext.Root.Result?.ToActionResult();
                        }
                    }
                    // Evaluate condition
                    step.ExecutionContext.Debug($"Evaluating condition for step: '{step.DisplayName}'");
                    var conditionTraceWriter = new ConditionTraceWriter(Trace, step.ExecutionContext);
                    var conditionResult = false;
                    var conditionEvaluateError = default(Exception);
                    if (HostContext.RunnerShutdownToken.IsCancellationRequested)
                    {
                        step.ExecutionContext.Debug($"Skip evaluate condition on runner shutdown.");
                    }
                    else
                    {
                        try
                        {
                            var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator(conditionTraceWriter);
                            var condition = new BasicExpressionToken(null, null, null, step.Condition);
                            conditionResult = templateEvaluator.EvaluateStepIf(condition, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions, step.ExecutionContext.ToExpressionState());
                        }
                        catch (Exception ex)
                        {
                            Trace.Info("Caught exception from expression.");
                            Trace.Error(ex);
                            conditionEvaluateError = ex;
                        }
                    }
                    if (!conditionResult && conditionEvaluateError == null)
                    {
                        // Condition is false
                        Trace.Info("Skipping step due to condition evaluation.");
                        SetStepConclusion(step, TaskResult.Skipped);
                        continue;
                    }
                    else if (conditionEvaluateError != null)
                    {
                        // Condition error
                        step.ExecutionContext.Error(conditionEvaluateError);
                        SetStepConclusion(step, TaskResult.Failed);
                        ExecutionContext.Result = TaskResult.Failed;
                        break;
                    }
                    else
                    {
                        await RunStepAsync(step);
                    }

                }
                finally
                {
                    if (jobCancelRegister != null)
                    {
                        jobCancelRegister?.Dispose();
                        jobCancelRegister = null;
                    }
                }
                // Check failed or cancelled
                if (step.ExecutionContext.Result == TaskResult.Failed || step.ExecutionContext.Result == TaskResult.Canceled)
                {
                    Trace.Info($"Update job result with current composite step result '{step.ExecutionContext.Result}'.");
                    ExecutionContext.Result = TaskResultUtil.MergeTaskResults(ExecutionContext.Result, step.ExecutionContext.Result.Value);
                }

                // Update context
                SetStepsContext(step);
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
                    SetStepConclusion(step, TaskResult.Failed);
                }
                else
                {
                    Trace.Error($"Caught cancellation exception from step: {ex}");
                    step.ExecutionContext.Error(ex);
                    SetStepConclusion(step, TaskResult.Canceled);
                }
            }
            catch (Exception ex)
            {
                // Log the error and fail the step
                Trace.Error($"Caught exception from step: {ex}");
                step.ExecutionContext.Error(ex);
                SetStepConclusion(step, TaskResult.Failed);
            }

            // Merge execution context result with command result
            if (step.ExecutionContext.CommandResult != null)
            {
                SetStepConclusion(step, Common.Util.TaskResultUtil.MergeTaskResults(step.ExecutionContext.Result, step.ExecutionContext.CommandResult.Value));
            }

            Trace.Info($"Step result: {step.ExecutionContext.Result}");
            step.ExecutionContext.Debug($"Finished: {step.DisplayName}");
            step.ExecutionContext.PublishStepTelemetry();
        }

        private void SetStepConclusion(IStep step, TaskResult result)
        {
            step.ExecutionContext.Result = result;
            SetStepsContext(step);
        }
        private void SetStepsContext(IStep step)
        {
            if (!string.IsNullOrEmpty(step.ExecutionContext.ContextName) && !step.ExecutionContext.ContextName.StartsWith("__", StringComparison.Ordinal))
            {
                // TODO: when we support continue on error, we may need to do logic here to change conclusion based on the continue on error result
                step.ExecutionContext.Global.StepsContext.SetOutcome(step.ExecutionContext.ScopeName, step.ExecutionContext.ContextName, (step.ExecutionContext.Result ?? TaskResult.Succeeded).ToActionResult());
                step.ExecutionContext.Global.StepsContext.SetConclusion(step.ExecutionContext.ScopeName, step.ExecutionContext.ContextName, (step.ExecutionContext.Result ?? TaskResult.Succeeded).ToActionResult());
            }
        }
    }
}
