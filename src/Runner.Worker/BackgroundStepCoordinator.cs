using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(BackgroundStepCoordinator))]
    public interface IBackgroundStepCoordinator : IRunnerService
    {
        void InitializeCoordinator(int maxConcurrent);
        void StartBackgroundStep(IStep step, CancellationToken jobCancellationToken);
        Task<TaskResult> WaitForUnwaitedStepsAsync(CancellationToken cancellationToken);
        Task RunControlFlowAsync(IExecutionContext stepContext, object data);
    }

    /// <summary>
    /// Coordinates background step execution, waiting, cancellation, and deferred state.
    /// Extracted from StepsRunner so the main step loop stays clean.
    /// </summary>
    public sealed class BackgroundStepCoordinator : RunnerService, IBackgroundStepCoordinator
    {
        private const int DefaultMaxBackgroundSteps = 10;
        private readonly Dictionary<string, (IStep Step, Task Task, CancellationTokenSource Cts)> _backgroundSteps = new();

        // IDs of background steps that have already been completed (waited on or canceled).
        // Used to avoid waiting on or flushing the same step more than once.
        private readonly HashSet<string> _completedStepIds = new();

        // IDs of background steps that were explicitly canceled via a `cancel` control step.
        // These steps are expected to be canceled, so their (Canceled) result must not be
        // merged into the overall job result.
        private readonly HashSet<string> _explicitlyCanceledStepIds = new();
        private SemaphoreSlim _backgroundSlotSemaphore = new SemaphoreSlim(DefaultMaxBackgroundSteps);

        /// <summary>
        /// Reset per-job state. Call at the start of each job.
        /// </summary>
        public void InitializeCoordinator(int maxConcurrent)
        {
            _backgroundSteps.Clear();
            _completedStepIds.Clear();
            _explicitlyCanceledStepIds.Clear();
            var max = maxConcurrent > 0 ? maxConcurrent : DefaultMaxBackgroundSteps;
            _backgroundSlotSemaphore = new SemaphoreSlim(max);
        }

        // -----------------------------------------------------------------
        // Starting background steps
        // -----------------------------------------------------------------

        /// <summary>
        /// Prepare and launch a background step. Does not block the caller.
        /// </summary>
        public void StartBackgroundStep(IStep step, CancellationToken jobCancellationToken)
        {
            var stepId = step.ExecutionContext?.ContextName ?? step.DisplayName;

            // Isolate GitHubContext so concurrent steps don't overwrite each other's GITHUB_OUTPUT paths
            if (step.ExecutionContext.ExpressionValues.TryGetValue("github", out var ghCtx) && ghCtx is GitHubContext sharedGitHub)
            {
                step.ExecutionContext.ExpressionValues["github"] = sharedGitHub.ShallowCopy();
            }

            var bgCts = CancellationTokenSource.CreateLinkedTokenSource(jobCancellationToken);

            // Evaluate timeout on the main thread (needs expression context)
            var timeoutMinutes = 0;
            try
            {
                var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();
                timeoutMinutes = templateEvaluator.EvaluateStepTimeout(step.Timeout, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions);
            }
            catch (Exception ex)
            {
                Trace.Info($"Error determining timeout for background step '{stepId}': {ex.Message}");
            }

            var task = ExecuteBackgroundStepCoreAsync(step, bgCts, stepId, timeoutMinutes);
            _backgroundSteps[stepId] = (step, task, bgCts);
            Trace.Info($"Background step '{stepId}' queued (slot will be acquired asynchronously).");
        }

        // -----------------------------------------------------------------
        // Safety net
        // -----------------------------------------------------------------

        // Drain any background steps that weren't already waited on by an explicit wait/cancel
        // control step, then merge the final results of all background steps into a single result
        // for the caller to fold into the job result.
        public async Task<TaskResult> WaitForUnwaitedStepsAsync(CancellationToken cancellationToken)
        {
            var unwaitedIds = _backgroundSteps.Keys.Where(id => !_completedStepIds.Contains(id)).ToList();
            if (unwaitedIds.Count > 0)
            {
                Trace.Info($"Safety net: {unwaitedIds.Count} unwaited background step(s) at post-job boundary: {string.Join(", ", unwaitedIds)}");
                await WaitForStepTasksAsync(unwaitedIds, cancellationToken);
                CompleteWaitedSteps(unwaitedIds);
            }

            var result = TaskResult.Succeeded;
            foreach (var (stepId, (step, _, _)) in _backgroundSteps)
            {
                // A step that succeeded does not set a Result by default, so a missing
                // value means the step succeeded and there is nothing to merge.
                if (!step.ExecutionContext.Result.HasValue)
                {
                    continue;
                }

                // A step explicitly canceled via a `cancel` control step is expected to be canceled,
                // so a Canceled result must not influence the overall job result. However, if the step
                // failed (e.g. before the cancellation took effect), that failure should still count.
                if (_explicitlyCanceledStepIds.Contains(stepId) &&
                    step.ExecutionContext.Result.Value == TaskResult.Canceled)
                {
                    continue;
                }

                result = TaskResultUtil.MergeTaskResults(result, step.ExecutionContext.Result.Value);
            }

            if (result != TaskResult.Succeeded)
            {
                Trace.Info($"Background steps reported result '{result}' to caller.");
            }

            return result;
        }

        // -----------------------------------------------------------------
        // Control-flow step dispatch
        // -----------------------------------------------------------------

        /// <summary>
        /// Execute a control-flow step (wait, wait-all, cancel) and propagate results.
        /// </summary>
        public async Task RunControlFlowAsync(IExecutionContext stepContext, object data)
        {
            var controlFlow = data as BackgroundStepControlFlowData;
            switch (controlFlow.Type)
            {
                case Pipelines.BackgroundControlTypes.Wait:
                {
                    var ids = controlFlow.StepIds ?? Array.Empty<string>();
                    stepContext.Output($"Waiting for background step(s) to complete: {DescribeSteps(ids)}");
                    await WaitForStepTasksAsync(ids, stepContext.CancellationToken);
                    stepContext.Result = CompleteWaitedSteps(ids);
                    ReportCompletedSteps(stepContext, "Finished waiting for background step(s).", ids);
                    break;
                }

                case Pipelines.BackgroundControlTypes.WaitAll:
                {
                    var remaining = _backgroundSteps.Keys.Where(id => !_completedStepIds.Contains(id)).ToList();
                    stepContext.Output(remaining.Count > 0
                        ? $"Waiting for all background step(s) to complete: {DescribeSteps(remaining)}"
                        : "No background steps remaining to wait for.");
                    await WaitForStepTasksAsync(remaining, stepContext.CancellationToken);
                    stepContext.Result = CompleteWaitedSteps(remaining);
                    ReportCompletedSteps(stepContext, "Finished waiting for all background step(s).", remaining);
                    break;
                }

                case Pipelines.BackgroundControlTypes.Cancel:
                {
                    var cancelIds = controlFlow.StepIds ?? Array.Empty<string>();
                    stepContext.Output($"Cancelling background step(s): {DescribeSteps(cancelIds)}");
                    await CancelStepsAsync(controlFlow.StepIds);
                    stepContext.Result = TaskResult.Succeeded;
                    ReportCompletedSteps(stepContext, "Finished cancelling background step(s).", cancelIds);
                    break;
                }

                default:
                    throw new ArgumentException($"Unknown background step control type '{controlFlow.Type}'.");
            }
        }

        // -----------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------

        // Resolve background step IDs to their display names for customer-facing output.
        private string DescribeSteps(IEnumerable<string> stepIds)
        {
            var names = stepIds
                .Select(id => _backgroundSteps.TryGetValue(id, out var entry) ? entry.Step.DisplayName : id)
                .ToList();
            return names.Count > 0 ? string.Join(", ", names) : "(none)";
        }

        // Emit a completion summary plus the final result of each affected background step.
        private void ReportCompletedSteps(IExecutionContext stepContext, string summary, IEnumerable<string> stepIds)
        {
            stepContext.Output(summary);
            foreach (var id in stepIds)
            {
                if (_backgroundSteps.TryGetValue(id, out var entry))
                {
                    var result = entry.Step.ExecutionContext.Result?.ToString() ?? "Unknown";
                    stepContext.Output($"  {entry.Step.DisplayName}: {result}");
                }
            }
        }

        private async Task ExecuteBackgroundStepCoreAsync(
            IStep step, CancellationTokenSource bgCts,
            string stepId, int timeoutMinutes)
        {
            Trace.Info($"Background step '{stepId}' waiting for slot.");
            await _backgroundSlotSemaphore.WaitAsync(bgCts.Token);
            Trace.Info($"Background step '{stepId}' acquired slot.");

            step.ExecutionContext.Start();

            if (timeoutMinutes > 0)
            {
                step.ExecutionContext.SetTimeout(TimeSpan.FromMinutes(timeoutMinutes));
            }

            using var cancelReg = bgCts.Token.Register(() =>
            {
                Trace.Info($"Background step '{stepId}': cancellation signalled, sending CancelToken to process.");
                step.ExecutionContext.CancelToken();
            });

            TaskResult? result = null;
            try
            {
                await step.RunAsync();
                result = step.ExecutionContext.Result ?? TaskResult.Succeeded;
            }
            catch (OperationCanceledException) when (bgCts.Token.IsCancellationRequested)
            {
                result = TaskResult.Canceled;
            }
            catch (OperationCanceledException) when (step.ExecutionContext.CancellationToken.IsCancellationRequested)
            {
                Trace.Info($"Background step '{stepId}' timed out after {timeoutMinutes} minutes.");
                step.ExecutionContext.Error($"The background step '{step.DisplayName}' has timed out after {timeoutMinutes} minutes.");
                result = TaskResult.Failed;
            }
            catch (Exception ex)
            {
                Trace.Info($"Background step '{stepId}' failed: {ex.Message}");
                step.ExecutionContext.Error(ex);
                result = TaskResult.Failed;
            }
            finally
            {
                _backgroundSlotSemaphore.Release();

                if (step.ExecutionContext.CommandResult != null)
                {
                    result = TaskResultUtil.MergeTaskResults(result, step.ExecutionContext.CommandResult.Value);
                }

                step.ExecutionContext.Result = result;
                step.ExecutionContext.ApplyContinueOnError(step.ContinueOnError);

                step.ExecutionContext.Complete(step.ExecutionContext.Result);
                Trace.Info($"Background step '{stepId}' completed with result: {step.ExecutionContext.Result}");
            }
        }

        private async Task CancelStepsAsync(string[] cancelStepIds)
        {
            if (cancelStepIds == null || cancelStepIds.Length == 0)
            {
                return;
            }

            // Mark these steps as expected-to-be-canceled so their result does not
            // affect the overall job result.
            foreach (var id in cancelStepIds)
            {
                _explicitlyCanceledStepIds.Add(id);
            }

            var idsToCancel = cancelStepIds
                .Where(id => _backgroundSteps.ContainsKey(id) && !_backgroundSteps[id].Task.IsCompleted)
                .ToArray();

            if (idsToCancel.Length > 0)
            {
                Trace.Info($"Cancelling {idsToCancel.Length} background step(s): {string.Join(", ", idsToCancel)}");
                await CancelWithGracePeriodAsync(idsToCancel);
            }

            // Flush deferred state and mark canceled steps as completed.
            CompleteWaitedSteps(cancelStepIds);
        }

        private async Task WaitForStepTasksAsync(IEnumerable<string> stepIds, CancellationToken cancellationToken)
        {
            var ids = stepIds.ToList();
            var tasks = new List<Task>();

            foreach (var stepId in ids)
            {
                if (_backgroundSteps.TryGetValue(stepId, out var entry) && !entry.Task.IsCompleted)
                {
                    tasks.Add(entry.Task);
                }
                else if (!_backgroundSteps.ContainsKey(stepId))
                {
                    Trace.Info($"Wait references unknown background step: {stepId}");
                }
            }

            if (tasks.Count > 0)
            {
                Trace.Info($"Waiting for {tasks.Count} background step(s)...");
                try
                {
                    await Task.WhenAll(tasks).WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Trace.Info("Wait interrupted by job cancellation — cancelling background steps.");
                    await CancelWithGracePeriodAsync(ids);
                }
            }
        }

        private async Task CancelWithGracePeriodAsync(IEnumerable<string> stepIds, double graceSeconds = 7.5)
        {
            var cancelledSteps = new List<(string StepId, Task Task, IStep Step)>();
            foreach (var stepId in stepIds)
            {
                if (_backgroundSteps.TryGetValue(stepId, out var entry) && !entry.Task.IsCompleted)
                {
                    entry.Step.ExecutionContext.CancelToken();
                    entry.Cts.Cancel();
                    cancelledSteps.Add((stepId, entry.Task, entry.Step));
                }
            }

            if (cancelledSteps.Count > 0)
            {
                try
                {
                    await Task.WhenAll(cancelledSteps.Select(s => s.Task)).WaitAsync(TimeSpan.FromSeconds(graceSeconds));
                }
                catch (TimeoutException)
                {
                    Trace.Info($"Some background steps did not terminate within {graceSeconds}s grace period.");

                    // The step tasks above never completed, so their finally block never ran and
                    // their result was never set. Force-mark them as canceled so the abandoned
                    // steps still report a terminal result.
                    foreach (var (stepId, task, step) in cancelledSteps)
                    {
                        if (!task.IsCompleted && !step.ExecutionContext.Result.HasValue)
                        {
                            step.ExecutionContext.Result = TaskResult.Canceled;
                            Trace.Info($"Background step '{stepId}' did not terminate within grace period; marking as canceled.");
                        }
                    }
                }
            }
        }

        private TaskResult CompleteWaitedSteps(IEnumerable<string> stepIds)
        {
            var result = TaskResult.Succeeded;
            foreach (var id in stepIds)
            {
                _completedStepIds.Add(id);
                if (_backgroundSteps.TryGetValue(id, out var entry))
                {
                    // Flush deferred state for the completed step.
                    entry.Step.ExecutionContext.FlushDeferredOutputs();
                    entry.Step.ExecutionContext.FlushDeferredEnvironment();
                    entry.Step.ExecutionContext.FlushDeferredOutcomeConclusion();
                    Trace.Info($"Flushed deferred state for background step '{id}'.");

                    if (entry.Step.ExecutionContext.Result.HasValue)
                    {
                        result = TaskResultUtil.MergeTaskResults(result, entry.Step.ExecutionContext.Result.Value);
                    }
                }
            }
            return result;
        }
    }
}
