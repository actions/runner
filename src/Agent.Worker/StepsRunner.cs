using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Expressions = Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IStep
    {
        string Condition { get; }
        // Treat Failed as SucceededWithIssues.
        bool ContinueOnError { get; }
        // Treat failure as fatal. Subsequent AlwaysRun steps will not run.
        bool Critical { get; }
        string DisplayName { get; }
        bool Enabled { get; }
        IExecutionContext ExecutionContext { get; set; }
        // Always runs. Even if a previous critical step failed.
        bool Finally { get; }
        TimeSpan? Timeout { get; }
        Task RunAsync();
    }

    [ServiceLocator(Default = typeof(StepsRunner))]
    public interface IStepsRunner : IAgentService
    {
        Task RunAsync(IExecutionContext jobContext, IList<IStep> steps);
    }

    public sealed class StepsRunner : AgentService, IStepsRunner
    {
        public async Task RunAsync(IExecutionContext jobContext, IList<IStep> steps)
        {
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNull(steps, nameof(steps));

            // TaskResult:
            //  Abandoned
            //  Canceled
            //  Failed
            //  Skipped
            //  Succeeded
            //  SucceededWithIssues
            bool stepFailed = false;
            bool criticalFailure = false;
            int stepCount = 0;
            jobContext.Variables.Agent_JobStatus = TaskResult.Succeeded;
            foreach (IStep step in steps)
            {
                Trace.Info($"Processing step: DisplayName='{step.DisplayName}', ContinueOnError={step.ContinueOnError}, Critical={step.Critical}, Enabled={step.Enabled}, Finally={step.Finally}");
                ArgUtil.Equal(true, step.Enabled, nameof(step.Enabled));
                ArgUtil.NotNull(step.ExecutionContext, nameof(step.ExecutionContext));
                ArgUtil.NotNull(step.ExecutionContext.Variables, nameof(step.ExecutionContext.Variables));

                jobContext.Progress(stepCount++ * 100 / steps.Count);

                // Start.
                step.ExecutionContext.Start();

                // Test critical failure.
                if (criticalFailure && !step.Finally)
                {
                    Trace.Info("Skipping step due to previous critical failure.");
                    step.ExecutionContext.Complete(TaskResult.Skipped);
                    continue;
                }

                // Variable expansion.
                List<string> expansionWarnings;
                step.ExecutionContext.Variables.RecalculateExpanded(out expansionWarnings);
                expansionWarnings?.ForEach(x => step.ExecutionContext.Warning(x));

                // Record the job cancellation state prior evaluating the condition.
                bool alreadyCanceled = jobContext.CancellationToken.IsCancellationRequested;

                // Evaluate condition.
                step.ExecutionContext.Debug($"Evaluating condition for step: '{step.DisplayName}'");
                var expressionManager = HostContext.GetService<IExpressionManager>();
                Expressions.INode conditionTree = null;
                bool? conditionResult = null;
                try
                {
                    conditionTree = expressionManager.Parse(step.ExecutionContext, step.Condition);
                    conditionResult = expressionManager.Evaluate(jobContext, step.ExecutionContext, conditionTree);
                }
                catch (Exception ex)
                {
                    Trace.Info("Caught exception from expression.");
                    Trace.Error(ex);
                    step.ExecutionContext.Error(ex);
                    step.ExecutionContext.Complete(TaskResult.Failed);
                    criticalFailure = true;
                }

                if (conditionResult != null)
                {
                    if (!conditionResult.Value)
                    {
                        // Condition == false
                        Trace.Info("Skipping step due to condition evaluation.");
                        step.ExecutionContext.Complete(TaskResult.Skipped);
                    }
                    else
                    {
                        // Run the step.
                        await RunStepAsync(jobContext, step, alreadyCanceled, conditionTree);
                    }
                }

                // Update the step failed flags.
                stepFailed = stepFailed || step.ExecutionContext.Result == TaskResult.Failed;
                criticalFailure = criticalFailure || (step.Critical && step.ExecutionContext.Result == TaskResult.Failed);

                // Update the job result.
                if (step.ExecutionContext.Result == TaskResult.Failed)
                {
                    jobContext.Result = TaskResult.Failed;
                    jobContext.Variables.Agent_JobStatus = TaskResult.Failed;
                }
                else if ((jobContext.Result ?? TaskResult.Succeeded) == TaskResult.Succeeded &&
                    step.ExecutionContext.Result == TaskResult.SucceededWithIssues)
                {
                    jobContext.Result = TaskResult.SucceededWithIssues;
                    jobContext.Variables.Agent_JobStatus = TaskResult.SucceededWithIssues;
                }

                Trace.Info($"Current state: job state = '{jobContext.Result}', step failed = {stepFailed}, critical failure = {criticalFailure}");
            }
        }

        private async Task RunStepAsync(IExecutionContext jobContext, IStep step, bool alreadyCanceled, Expressions.INode conditionTree)
        {
            // Start the step.
            Trace.Info("Starting the step.");
            step.ExecutionContext.Section(StringUtil.Loc("StepStarting", step.DisplayName));
            step.ExecutionContext.SetTimeout(timeout: step.Timeout);
            Task stepTask = step.RunAsync();

            // Re-evaluate the condition when the job cancellation token is fired.
            if (!alreadyCanceled)
            {
                CancellationTokenSource evaluateTokenSource = null;
                try
                {
                    // Do not use the job cancellation token directly for the delay task.
                    // Otherwise, a delay task is leaked for each step.
                    evaluateTokenSource = new CancellationTokenSource();
                    Task evaluateDelay = Task.Delay(-1, evaluateTokenSource.Token);
                    using (var registration = jobContext.CancellationToken.Register(() => evaluateTokenSource.Cancel()))
                    {
                        Task completedTask = await Task.WhenAny(new[] { stepTask, evaluateDelay });
                        if (completedTask == evaluateDelay)
                        {
                            // Test the condition again. The job was canceled after the condition was originally evaluated.
                            var expressionManager = HostContext.GetService<IExpressionManager>();
                            // todo: what if evaluation fails here? if treat as critical then need to: 1) log the error (after task completes?) and 2) workaround continue-on-error logic below and 3) bubble flag back to caller to indicate critical error occurred
                            if (!expressionManager.Evaluate(jobContext, step.ExecutionContext, conditionTree, hostTracingOnly: true))
                            {
                                // Cancel the step.
                                step.ExecutionContext.CancelToken();
                            }
                        }
                    }
                }
                finally
                {
                    evaluateTokenSource?.Cancel();
                    evaluateTokenSource?.Dispose();
                }
            }

            List<OperationCanceledException> allCancelExceptions = new List<OperationCanceledException>();
            try
            {
                await stepTask;
            }
            catch (OperationCanceledException ex)
            {
                if (step.ExecutionContext.CancellationToken.IsCancellationRequested &&
                    !jobContext.CancellationToken.IsCancellationRequested)
                {
                    Trace.Error($"Caught timeout exception from step: {ex.Message}");
                    step.ExecutionContext.Error(StringUtil.Loc("StepTimedOut"));
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

            // Wait till all async commands finish.
            foreach (var command in step.ExecutionContext.AsyncCommands ?? new List<IAsyncCommandContext>())
            {
                try
                {
                    // wait async command to finish.
                    await command.WaitAsync();
                }
                catch (OperationCanceledException ex)
                {
                    if (step.ExecutionContext.CancellationToken.IsCancellationRequested &&
                        !jobContext.CancellationToken.IsCancellationRequested)
                    {
                        // Log the timeout error, set step result to falied if the current result is not canceled.
                        Trace.Error($"Caught timeout exception from async command {command.Name}: {ex}");
                        step.ExecutionContext.Error(StringUtil.Loc("StepTimedOut"));

                        // if the step already canceled, don't set it to failed.
                        step.ExecutionContext.CommandResult = TaskResultUtil.MergeTaskResults(step.ExecutionContext.CommandResult, TaskResult.Failed);
                    }
                    else
                    {
                        // log and save the OperationCanceledException, set step result to canceled if the current result is not failed.
                        Trace.Error($"Caught cancellation exception from async command {command.Name}: {ex}");
                        step.ExecutionContext.Error(ex);

                        // if the step already failed, don't set it to canceled.
                        step.ExecutionContext.CommandResult = TaskResultUtil.MergeTaskResults(step.ExecutionContext.CommandResult, TaskResult.Canceled);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error, set step result to falied if the current result is not canceled.
                    Trace.Error($"Caught exception from async command {command.Name}: {ex}");
                    step.ExecutionContext.Error(ex);

                    // if the step already canceled, don't set it to failed.
                    step.ExecutionContext.CommandResult = TaskResultUtil.MergeTaskResults(step.ExecutionContext.CommandResult, TaskResult.Failed);
                }
            }

            // Merge executioncontext result with command result
            if (step.ExecutionContext.CommandResult != null)
            {
                step.ExecutionContext.Result = TaskResultUtil.MergeTaskResults(step.ExecutionContext.Result, step.ExecutionContext.CommandResult.Value);
            }

            // Fixup the step result if ContinueOnError.
            if (step.ExecutionContext.Result == TaskResult.Failed && step.ContinueOnError)
            {
                step.ExecutionContext.Result = TaskResult.SucceededWithIssues;
                Trace.Info($"Updated step result: {step.ExecutionContext.Result}");
            }
            else
            {
                Trace.Info($"Step result: {step.ExecutionContext.Result}");
            }

            // Complete the step context.
            step.ExecutionContext.Section(StringUtil.Loc("StepFinishing", step.DisplayName));
            step.ExecutionContext.Complete();
        }
    }
}
