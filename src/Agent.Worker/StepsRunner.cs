using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface IStep
    {
        IExpressionNode Condition { get; set; }
        bool ContinueOnError { get; }
        string DisplayName { get; }
        bool Enabled { get; }
        IExecutionContext ExecutionContext { get; set; }
        TimeSpan? Timeout { get; }
        Task RunAsync();
    }

    [ServiceLocator(Default = typeof(StepsRunner))]
    public interface IStepsRunner : IAgentService
    {
        Task RunAsync(IExecutionContext Context, IList<IStep> steps);
    }

    public sealed class StepsRunner : AgentService, IStepsRunner
    {
        // StepsRunner should never throw exception to caller
        public async Task RunAsync(IExecutionContext jobContext, IList<IStep> steps)
        {
            ArgUtil.NotNull(jobContext, nameof(jobContext));
            ArgUtil.NotNull(steps, nameof(steps));

            // TaskResult:
            //  Abandoned (Server set this.)
            //  Canceled
            //  Failed
            //  Skipped
            //  Succeeded
            //  SucceededWithIssues
            CancellationTokenRegistration? jobCancelRegister = null;
            int stepIndex = 0;
            jobContext.Variables.Agent_JobStatus = jobContext.Result ?? TaskResult.Succeeded;
            foreach (IStep step in steps)
            {
                Trace.Info($"Processing step: DisplayName='{step.DisplayName}', ContinueOnError={step.ContinueOnError}, Enabled={step.Enabled}");
                ArgUtil.Equal(true, step.Enabled, nameof(step.Enabled));
                ArgUtil.NotNull(step.ExecutionContext, nameof(step.ExecutionContext));
                ArgUtil.NotNull(step.ExecutionContext.Variables, nameof(step.ExecutionContext.Variables));
                stepIndex++;

                // Start.
                step.ExecutionContext.Start();
                var taskStep = step as ITaskRunner;
                if (taskStep != null)
                {
                    HostContext.WritePerfCounter($"TaskStart_{taskStep.Task.Reference.Name}_{stepIndex}");
                }

                // Variable expansion.
                List<string> expansionWarnings;
                step.ExecutionContext.Variables.RecalculateExpanded(out expansionWarnings);
                expansionWarnings?.ForEach(x => step.ExecutionContext.Warning(x));

                var expressionManager = HostContext.GetService<IExpressionManager>();
                try
                {
                    // Register job cancellation call back only if job cancellation token not been fire before each step run
                    if (!jobContext.CancellationToken.IsCancellationRequested)
                    {
                        // Test the condition again. The job was canceled after the condition was originally evaluated.
                        jobCancelRegister = jobContext.CancellationToken.Register(() =>
                        {
                            // mark job as cancelled
                            jobContext.Result = TaskResult.Canceled;
                            jobContext.Variables.Agent_JobStatus = jobContext.Result;

                            step.ExecutionContext.Debug($"Re-evaluate condition on job cancellation for step: '{step.DisplayName}'.");
                            ConditionResult conditionReTestResult;
                            if (HostContext.AgentShutdownToken.IsCancellationRequested)
                            {
                                step.ExecutionContext.Debug($"Skip Re-evaluate condition on agent shutdown.");
                                conditionReTestResult = false;
                            }
                            else
                            {
                                try
                                {
                                    conditionReTestResult = expressionManager.Evaluate(step.ExecutionContext, step.Condition, hostTracingOnly: true);
                                }
                                catch (Exception ex)
                                {
                                    // Cancel the step since we get exception while re-evaluate step condition.
                                    Trace.Info("Caught exception from expression when re-test condition on job cancellation.");
                                    step.ExecutionContext.Error(ex);
                                    conditionReTestResult = false;
                                }
                            }

                            if (!conditionReTestResult.Value)
                            {
                                // Cancel the step.
                                Trace.Info("Cancel current running step.");
                                step.ExecutionContext.CancelToken();
                            }
                        });
                    }
                    else
                    {
                        if (jobContext.Result != TaskResult.Canceled)
                        {
                            // mark job as cancelled
                            jobContext.Result = TaskResult.Canceled;
                            jobContext.Variables.Agent_JobStatus = jobContext.Result;
                        }
                    }

                    // Evaluate condition.
                    step.ExecutionContext.Debug($"Evaluating condition for step: '{step.DisplayName}'");
                    Exception conditionEvaluateError = null;
                    ConditionResult conditionResult;
                    if (HostContext.AgentShutdownToken.IsCancellationRequested)
                    {
                        step.ExecutionContext.Debug($"Skip evaluate condition on agent shutdown.");
                        conditionResult = false;
                    }
                    else
                    {
                        try
                        {
                            conditionResult = expressionManager.Evaluate(step.ExecutionContext, step.Condition);
                        }
                        catch (Exception ex)
                        {
                            Trace.Info("Caught exception from expression.");
                            Trace.Error(ex);
                            conditionResult = false;
                            conditionEvaluateError = ex;
                        }
                    }

                    // no evaluate error but condition is false
                    if (!conditionResult.Value && conditionEvaluateError == null)
                    {
                        // Condition == false
                        Trace.Info("Skipping step due to condition evaluation.");
                        step.ExecutionContext.Complete(TaskResult.Skipped, resultCode: conditionResult.Trace);
                        continue;
                    }

                    if (conditionEvaluateError != null)
                    {
                        // fail the step since there is an evaluate error.
                        step.ExecutionContext.Error(conditionEvaluateError);
                        step.ExecutionContext.Complete(TaskResult.Failed);
                    }
                    else
                    {
                        // Run the step.
                        await RunStepAsync(step, jobContext.CancellationToken);
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

                // Update the job result.
                if (step.ExecutionContext.Result == TaskResult.SucceededWithIssues ||
                    step.ExecutionContext.Result == TaskResult.Failed)
                {
                    Trace.Info($"Update job result with current step result '{step.ExecutionContext.Result}'.");
                    jobContext.Result = TaskResultUtil.MergeTaskResults(jobContext.Result, step.ExecutionContext.Result.Value);
                    jobContext.Variables.Agent_JobStatus = jobContext.Result;
                }
                else
                {
                    Trace.Info($"No need for updating job result with current step result '{step.ExecutionContext.Result}'.");
                }

                if (taskStep != null)
                {
                    HostContext.WritePerfCounter($"TaskCompleted_{taskStep.Task.Reference.Name}_{stepIndex}");
                }

                Trace.Info($"Current state: job state = '{jobContext.Result}'");
            }
        }

        private async Task RunStepAsync(IStep step, CancellationToken jobCancellationToken)
        {
            // Start the step.
            Trace.Info("Starting the step.");
            step.ExecutionContext.Section(StringUtil.Loc("StepStarting", step.DisplayName));
            step.ExecutionContext.SetTimeout(timeout: step.Timeout);

#if OS_WINDOWS
            if (step.ExecutionContext.Variables.Retain_Default_Encoding != true && Console.InputEncoding.CodePage != 65001)
            {
                using (var p = HostContext.CreateService<IProcessInvoker>())
                {
                    // Use UTF8 code page
                    int exitCode = await p.ExecuteAsync(workingDirectory: HostContext.GetDirectory(WellKnownDirectory.Work),
                                            fileName: WhichUtil.Which("chcp", true, Trace),
                                            arguments: "65001",
                                            environment: null,
                                            requireExitCodeZero: false,
                                            outputEncoding: null,
                                            killProcessOnCancel: false,
                                            redirectStandardIn: null,
                                            inheritConsoleHandler: true,
                                            cancellationToken: step.ExecutionContext.CancellationToken);
                    if (exitCode == 0)
                    {
                        Trace.Info("Successfully returned to code page 65001 (UTF8)");
                    }
                    else
                    {
                        Trace.Warning($"'chcp 65001' failed with exit code {exitCode}");
                    }
                }
            }
#endif

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
                        !jobCancellationToken.IsCancellationRequested)
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
