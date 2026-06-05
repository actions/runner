using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
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
        Task WaitForUnwaitedStepsAsync(IExecutionContext jobContext);
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
        private readonly HashSet<string> _waitedStepIds = new();
        private SemaphoreSlim _backgroundSlotSemaphore = new SemaphoreSlim(DefaultMaxBackgroundSteps);

        /// <summary>
        /// Reset per-job state. Call at the start of each job.
        /// </summary>
        public void InitializeCoordinator(int maxConcurrent)
        {
            _backgroundSteps.Clear();
            _waitedStepIds.Clear();
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

        // -----------------------------------------------------------------
        // Wait
        // -----------------------------------------------------------------

        /// <summary>
        /// Wait for specific background steps by ID. Flushes deferred state and
        /// returns the aggregate result.
        /// </summary>
        public async Task<TaskResult> WaitForStepsAsync(string[] stepIds, CancellationToken cancellationToken)
        {
            var ids = stepIds ?? Array.Empty<string>();

            await WaitForStepTasksAsync(ids, cancellationToken);

            foreach (var id in ids)
            {
                FlushDeferredState(id);
                _waitedStepIds.Add(id);
            }

            return GetWaitResult(ids);
        }

        /// <summary>
        /// Wait for all not-yet-waited background steps. Returns the aggregate result.
        /// </summary>
        public async Task<TaskResult> WaitForAllAsync(CancellationToken cancellationToken)
        {
            var remaining = _backgroundSteps.Keys.Where(id => !_waitedStepIds.Contains(id)).ToList();

            await WaitForStepTasksAsync(remaining, cancellationToken);

            foreach (var id in remaining)
            {
                FlushDeferredState(id);
                _waitedStepIds.Add(id);
            }

            return GetWaitResult(remaining);
        }

        // -----------------------------------------------------------------
        // Cancel
        // -----------------------------------------------------------------

        /// <summary>
        /// Cancel specific background steps by ID. Flushes deferred state after cancellation.
        /// </summary>
        public async Task CancelStepsAsync(string[] cancelStepIds)
        {
            if (cancelStepIds == null || cancelStepIds.Length == 0) return;

            var idsToCancel = cancelStepIds
                .Where(id => _backgroundSteps.ContainsKey(id) && !_backgroundSteps[id].Task.IsCompleted)
                .ToArray();

            if (idsToCancel.Length > 0)
            {
                Trace.Info($"Cancelling {idsToCancel.Length} background step(s): {string.Join(", ", idsToCancel)}");
                await CancelWithGracePeriodAsync(idsToCancel);
            }

            foreach (var id in cancelStepIds)
            {
                FlushDeferredState(id);
            }
        }

        // -----------------------------------------------------------------
        // Failure propagation
        // -----------------------------------------------------------------

        /// <summary>
        /// Safety net: wait for any unwaited background steps and propagate failures.
        /// </summary>
        public async Task WaitForUnwaitedStepsAsync(IExecutionContext jobContext)
        {
            var unwaitedIds = _backgroundSteps.Keys.Where(id => !_waitedStepIds.Contains(id)).ToList();
            if (unwaitedIds.Count == 0)
            {
                return;
            }

            Trace.Info($"Safety net: {unwaitedIds.Count} unwaited background step(s) at post-job boundary: {string.Join(", ", unwaitedIds)}");
            await WaitForAllAsync(jobContext.CancellationToken);
            PropagateFailures(jobContext);
        }

        /// <summary>
        /// Propagate any unhandled background step failures to the job result.
        /// </summary>
        public void PropagateFailures(IExecutionContext jobContext)
        {
            foreach (var (_, (step, _, _)) in _backgroundSteps)
            {
                if (step.ExecutionContext.Result == TaskResult.Failed)
                {
                    Trace.Info($"Propagating failure from background step '{step.ExecutionContext.ContextName}' to job result.");
                    jobContext.Result = TaskResultUtil.MergeTaskResults(jobContext.Result, TaskResult.Failed);
                    jobContext.JobContext.Status = jobContext.Result?.ToActionResult();
                    break;
                }
            }
        }

        // -----------------------------------------------------------------
        // Control-flow step dispatch
        // -----------------------------------------------------------------

        /// <summary>
        /// Execute a control-flow step (wait, wait-all, cancel) and propagate results.
        /// </summary>
        public async Task RunControlFlowAsync(IExecutionContext stepContext, object data)
        {
            var controlFlow = data as ControlFlowStepData;
            switch (controlFlow.Type)
            {
                case Pipelines.BackgroundControlTypes.Wait:
                    stepContext.Result = await WaitForStepsAsync(controlFlow.StepIds, stepContext.CancellationToken);
                    break;

                case Pipelines.BackgroundControlTypes.WaitAll:
                    stepContext.Result = await WaitForAllAsync(stepContext.CancellationToken);
                    break;

                case Pipelines.BackgroundControlTypes.Cancel:
                    await CancelStepsAsync(controlFlow.StepIds);
                    stepContext.Result = TaskResult.Succeeded;
                    break;

                default:
                    throw new ArgumentException($"Unknown background step control type '{controlFlow.Type}'.");
            }
        }

        // -----------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------

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
            var tasks = new List<Task>();
            foreach (var stepId in stepIds)
            {
                if (_backgroundSteps.TryGetValue(stepId, out var entry) && !entry.Task.IsCompleted)
                {
                    entry.Step.ExecutionContext.CancelToken();
                    entry.Cts.Cancel();
                    tasks.Add(entry.Task);
                }
            }

            if (tasks.Count > 0)
            {
                try
                {
                    await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(graceSeconds));
                }
                catch (TimeoutException)
                {
                    Trace.Info($"Some background steps did not terminate within {graceSeconds}s grace period.");
                }
            }
        }

        private void FlushDeferredState(string stepId)
        {
            if (_backgroundSteps.TryGetValue(stepId, out var entry))
            {
                entry.Step.ExecutionContext.FlushDeferredOutputs();
                entry.Step.ExecutionContext.FlushDeferredEnvironment();
                entry.Step.ExecutionContext.FlushDeferredOutcomeConclusion();
                Trace.Info($"Flushed deferred state for background step '{stepId}'.");
            }
        }

        private TaskResult GetWaitResult(IEnumerable<string> stepIds)
        {
            if (stepIds == null) return TaskResult.Succeeded;

            foreach (var stepId in stepIds)
            {
                if (_backgroundSteps.TryGetValue(stepId, out var entry) && entry.Step.ExecutionContext.Result == TaskResult.Failed)
                {
                    Trace.Info($"Background step '{stepId}' failed.");
                    return TaskResult.Failed;
                }
            }
            return TaskResult.Succeeded;
        }
    }
}
