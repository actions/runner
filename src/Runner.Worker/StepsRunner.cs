using System;
using System.Collections.Generic;
using System.Linq;
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
using GitHub.Runner.Worker.Dap;
using GitHub.Runner.Worker.Expressions;

namespace GitHub.Runner.Worker
{
    public interface IStep
    {
        string Condition { get; set; }
        TemplateToken ContinueOnError { get; }
        string DisplayName { get; set; }
        IExecutionContext ExecutionContext { get; set; }
        TemplateToken Timeout { get; }
        bool TryUpdateDisplayName(out bool updated);
        bool EvaluateDisplayName(DictionaryContextData contextData, IExecutionContext context, out bool updated);
        Task RunAsync();
    }

    [ServiceLocator(Default = typeof(StepsRunner))]
    public interface IStepsRunner : IRunnerService
    {
        Task RunAsync(IExecutionContext Context);
    }

    public sealed class StepsRunner : RunnerService, IStepsRunner
    {
        // StepsRunner should never throw exception to caller
        public async Task RunAsync(IExecutionContext jobContext)
        {
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNull(jobContext.JobSteps, nameof(jobContext.JobSteps));

            // TaskResult:
            //  Abandoned (Server set this.)
            //  Canceled
            //  Failed
            //  Skipped
            //  Succeeded
            CancellationTokenRegistration? jobCancelRegister = null;
            jobContext.JobContext.Status = (jobContext.Result ?? TaskResult.Succeeded).ToActionResult();
            var scopeInputs = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
            bool checkPostJobActions = false;

            // Get debug session for DAP debugging support
            // The session's IsActive property determines if debugging is actually enabled
            var debugSession = HostContext.GetService<IDapDebugSession>();
            bool isFirstStep = true;
            int stepIndex = 0; // Track step index for checkpoints

            while (jobContext.JobSteps.Count > 0 || !checkPostJobActions)
            {
                if (jobContext.JobSteps.Count == 0 && !checkPostJobActions)
                {
                    checkPostJobActions = true;
                    while (jobContext.PostJobSteps.TryPop(out var postStep))
                    {
                        jobContext.JobSteps.Enqueue(postStep);
                    }

                    continue;
                }

                var step = jobContext.JobSteps.Dequeue();

                // Capture remaining steps for potential checkpoint (before we modify the queue)
                var remainingSteps = jobContext.JobSteps.ToList();

                Trace.Info($"Processing step: DisplayName='{step.DisplayName}'");
                ArgUtil.NotNull(step.ExecutionContext, nameof(step.ExecutionContext));
                ArgUtil.NotNull(step.ExecutionContext.Global, nameof(step.ExecutionContext.Global));
                ArgUtil.NotNull(step.ExecutionContext.Global.Variables, nameof(step.ExecutionContext.Global.Variables));

                // Start
                step.ExecutionContext.Start();

                // Expression functions
                // Clear first to handle step-back scenarios where the same step may be re-processed
                step.ExecutionContext.ExpressionFunctions.Clear();
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<HashFilesFunction>(PipelineTemplateConstants.HashFiles, 1, byte.MaxValue));

                // Expression values
                step.ExecutionContext.ExpressionValues["steps"] = step.ExecutionContext.Global.StepsContext.GetScope(step.ExecutionContext.ScopeName);
#if OS_WINDOWS
                var envContext = new DictionaryContextData();
#else
                var envContext = new CaseSensitiveDictionaryContextData();
#endif
                step.ExecutionContext.ExpressionValues["env"] = envContext;

                // Merge global env
                foreach (var pair in step.ExecutionContext.Global.EnvironmentVariables)
                {
                    envContext[pair.Key] = new StringContextData(pair.Value ?? string.Empty);
                }

                bool evaluateStepEnvFailed = false;
                if (step is IActionRunner actionStep)
                {
                    // Set GITHUB_ACTION
                    step.ExecutionContext.SetGitHubContext("action", actionStep.Action.Name);

                    try
                    {
                        // Evaluate and merge step env
                        var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();
                        var actionEnvironment = templateEvaluator.EvaluateStepEnvironment(actionStep.Action.Environment, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions, VarUtil.EnvironmentVariableKeyComparer);
                        foreach (var env in actionEnvironment)
                        {
                            envContext[env.Key] = new StringContextData(env.Value ?? string.Empty);
                            step.ExecutionContext.StepEnvironmentOverrides.Add(env.Key);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Fail the step since there is an evaluate error
                        Trace.Info("Caught exception from expression for step.env");
                        evaluateStepEnvFailed = true;
                        step.ExecutionContext.Error(ex);
                        CompleteStep(step, TaskResult.Failed);
                    }
                }

                if (!evaluateStepEnvFailed)
                {
                    try
                    {
                        // Register job cancellation call back only if job cancellation token not been fire before each step run
                        if (!jobContext.CancellationToken.IsCancellationRequested)
                        {
                            // Test the condition again. The job was cancelled after the condition was originally evaluated.
                            jobCancelRegister = jobContext.CancellationToken.Register(() =>
                            {
                                // Mark job as Cancelled or Failed depending on HostContext shutdown token's cancellation
                                jobContext.Result = HostContext.RunnerShutdownToken.IsCancellationRequested
                                                    ? TaskResult.Failed
                                                    : TaskResult.Canceled;
                                jobContext.JobContext.Status = jobContext.Result?.ToActionResult();

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
                            if (jobContext.Result != TaskResult.Canceled)
                            {
                                // Mark job as Cancelled or Failed depending on HostContext shutdown token's cancellation
                                jobContext.Result = HostContext.RunnerShutdownToken.IsCancellationRequested
                                    ? TaskResult.Failed
                                    : TaskResult.Canceled;
                                jobContext.JobContext.Status = jobContext.Result?.ToActionResult();
                            }
                        }

                        // Pause for DAP debugger BEFORE step execution
                        // This happens after expression values are set up so the debugger can inspect variables
                        if (debugSession?.IsActive == true)
                        {
                            // Store step info for checkpoint creation later
                            debugSession.SetPendingStepInfo(step, jobContext, stepIndex, remainingSteps);

                            // Pause and wait for user command (next/continue/stepBack/reverseContinue)
                            await debugSession.OnStepStartingAsync(step, jobContext, isFirstStep, jobContext.CancellationToken);
                            isFirstStep = false;

                            // Check if user requested to step back
                            if (debugSession.HasPendingRestore)
                            {
                                var checkpoint = debugSession.ConsumeRestoredCheckpoint();
                                if (checkpoint != null)
                                {
                                    // Restore the checkpoint state using the correct checkpoint index
                                    debugSession.RestoreCheckpoint(checkpoint.CheckpointIndex, jobContext);

                                    // Re-queue the steps from checkpoint
                                    while (jobContext.JobSteps.Count > 0)
                                    {
                                        jobContext.JobSteps.Dequeue();
                                    }

                                    // Queue the checkpoint's step and remaining steps
                                    // Reset execution context for rerun since CancellationTokenSource was disposed in Complete()
                                    checkpoint.CurrentStep.ExecutionContext.ResetForRerun();
                                    jobContext.JobSteps.Enqueue(checkpoint.CurrentStep);
                                    foreach (var remainingStep in checkpoint.RemainingSteps)
                                    {
                                        remainingStep.ExecutionContext.ResetForRerun();
                                        jobContext.JobSteps.Enqueue(remainingStep);
                                    }

                                    // Reset step index to checkpoint's index
                                    stepIndex = checkpoint.StepIndex;

                                    // Clear pending step info since we're not executing this step
                                    debugSession.ClearPendingStepInfo();

                                    // Skip to next iteration - will process restored step
                                    continue;
                                }
                            }

                            // User pressed next/continue - create checkpoint NOW
                            // This captures any REPL modifications made while paused
                            if (debugSession.ShouldCreateCheckpoint())
                            {
                                debugSession.CreateCheckpointForPendingStep(jobContext);
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
                            // This is our last, best chance to expand the display name.  (At this point, all the requirements for successful expansion should be met.)
                            // That being said, evaluating the display name should still be considered as a "best effort" exercise.  (It's not critical or paramount.)
                            // For that reason, we call a safe "Try..." wrapper method to ensure that any potential problems we encounter in evaluating the display name
                            // don't interfere with our ultimate goal within this code block:  evaluation of the condition.
                            step.TryUpdateDisplayName(out _);

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
                            CompleteStep(step, TaskResult.Skipped, resultCode: conditionTraceWriter.Trace);
                        }
                        else if (conditionEvaluateError != null)
                        {
                            // Condition error
                            step.ExecutionContext.Error(conditionEvaluateError);
                            CompleteStep(step, TaskResult.Failed);
                        }
                        else
                        {
                            // Run the step
                            await RunStepAsync(step, jobContext.CancellationToken);
                            CompleteStep(step);
                        }
                    }
                    finally
                    {
                        if (jobCancelRegister != null)
                        {
                            jobCancelRegister?.Dispose();
                            jobCancelRegister = null;
                        }

                        // Clear pending step info after step completes
                        debugSession?.ClearPendingStepInfo();
                    }
                }

                // Update the job result
                if (step.ExecutionContext.Result == TaskResult.Failed)
                {
                    Trace.Info($"Update job result with current step result '{step.ExecutionContext.Result}'.");
                    jobContext.Result = TaskResultUtil.MergeTaskResults(jobContext.Result, step.ExecutionContext.Result.Value);
                    jobContext.JobContext.Status = jobContext.Result?.ToActionResult();
                }
                else
                {
                    Trace.Info($"No need for updating job result with current step result '{step.ExecutionContext.Result}'.");
                }

                // Notify DAP debugger AFTER step execution
                if (debugSession?.IsActive == true)
                {
                    debugSession.OnStepCompleted(step);
                }

                // Increment step index for checkpoint tracking
                stepIndex++;

                Trace.Info($"Current state: job state = '{jobContext.Result}'");
            }

            // Notify DAP debugger that the job has completed
            debugSession?.OnJobCompleted();
        }

        private async Task RunStepAsync(IStep step, CancellationToken jobCancellationToken)
        {
            // Start the step
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

            await EncodingUtil.SetEncoding(HostContext, Trace, step.ExecutionContext.CancellationToken);

            try
            {
                await step.RunAsync();
            }
            catch (OperationCanceledException ex)
            {
                if (step.ExecutionContext.CancellationToken.IsCancellationRequested &&
                    !jobCancellationToken.IsCancellationRequested)
                {
                    Trace.Error($"Caught timeout exception from step: {ex.Message}");
                    step.ExecutionContext.Error($"The action '{step.DisplayName}' has timed out after {timeoutMinutes} minutes.");
                    step.ExecutionContext.Result = TaskResult.Failed;
                }
                else
                {
                    // Log the exception and cancel the step
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
                step.ExecutionContext.Result = TaskResultUtil.MergeTaskResults(step.ExecutionContext.Result, step.ExecutionContext.CommandResult.Value);
            }

            step.ExecutionContext.ApplyContinueOnError(step.ContinueOnError);

            Trace.Info($"Step result: {step.ExecutionContext.Result}");

            // Complete the step context
            step.ExecutionContext.Debug($"Finishing: {step.DisplayName}");
        }

        private void CompleteStep(IStep step, TaskResult? result = null, string resultCode = null)
        {
            var executionContext = step.ExecutionContext;

            executionContext.Complete(result, resultCode: resultCode);
        }
    }
}
