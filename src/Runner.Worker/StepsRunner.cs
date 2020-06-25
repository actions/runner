using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using GitHub.Runner.Worker.Expressions;
using ObjectTemplating = GitHub.DistributedTask.ObjectTemplating;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Worker
{
    public interface IStep
    {
        string Condition { get; set; }
        TemplateToken ContinueOnError { get; }
        string DisplayName { get; set; }
        IExecutionContext ExecutionContext { get; set; }
        TemplateToken Timeout { get; }
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
            while (jobContext.JobSteps.Count > 0 || !checkPostJobActions)
            {
                if (jobContext.JobSteps.Count == 0 && !checkPostJobActions)
                {
                    checkPostJobActions = true;
                    while (jobContext.PostJobSteps.TryPop(out var postStep))
                    {
                        jobContext.JobSteps.Add(postStep);
                    }

                    continue;
                }

                var step = jobContext.JobSteps[0];
                jobContext.JobSteps.RemoveAt(0);
                var nextStep = jobContext.JobSteps.Count > 0 ? jobContext.JobSteps[0] : null;

                Trace.Info($"Processing step: DisplayName='{step.DisplayName}'");
                ArgUtil.NotNull(step.ExecutionContext, nameof(step.ExecutionContext));
                ArgUtil.NotNull(step.ExecutionContext.Variables, nameof(step.ExecutionContext.Variables));

                // Start
                step.ExecutionContext.Start();

                // Expression functions
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<AlwaysFunction>(PipelineTemplateConstants.Always, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<CancelledFunction>(PipelineTemplateConstants.Cancelled, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<FailureFunction>(PipelineTemplateConstants.Failure, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<SuccessFunction>(PipelineTemplateConstants.Success, 0, 0));
                step.ExecutionContext.ExpressionFunctions.Add(new FunctionInfo<HashFilesFunction>(PipelineTemplateConstants.HashFiles, 1, byte.MaxValue));

                // Populate env context for each step
                Trace.Info("Initialize Env context for step");
#if OS_WINDOWS
                var envContext = new DictionaryContextData();
#else
                var envContext = new CaseSensitiveDictionaryContextData();
#endif
                step.ExecutionContext.ExpressionValues["env"] = envContext;
                foreach (var pair in step.ExecutionContext.EnvironmentVariables)
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
                        // Evaluate and merge action's env block to env context
                        var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();
                        var actionEnvironment = templateEvaluator.EvaluateStepEnvironment(actionStep.Action.Environment, step.ExecutionContext.ExpressionValues, step.ExecutionContext.ExpressionFunctions, VarUtil.EnvironmentVariableKeyComparer);
                        foreach (var env in actionEnvironment)
                        {
                            envContext[env.Key] = new StringContextData(env.Value ?? string.Empty);
                        }
                    }
                    catch (Exception ex)
                    {
                        // fail the step since there is an evaluate error.
                        Trace.Info("Caught exception from expression for step.env");
                        evaluateStepEnvFailed = true;
                        step.ExecutionContext.Error(ex);
                        CompleteStep(step, nextStep, TaskResult.Failed);
                    }
                }

                if (!evaluateStepEnvFailed)
                {
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
                                        // Cancel the step since we get exception while re-evaluate step condition.
                                        Trace.Info("Caught exception from expression when re-test condition on job cancellation.");
                                        step.ExecutionContext.Error(ex);
                                    }
                                }

                                if (!conditionReTestResult)
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
                                jobContext.JobContext.Status = jobContext.Result?.ToActionResult();
                            }
                        }

                        // Evaluate condition.
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

                        // no evaluate error but condition is false
                        if (!conditionResult && conditionEvaluateError == null)
                        {
                            // Condition == false
                            Trace.Info("Skipping step due to condition evaluation.");
                            CompleteStep(step, nextStep, TaskResult.Skipped, resultCode: conditionTraceWriter.Trace);
                        }
                        else if (conditionEvaluateError != null)
                        {
                            // fail the step since there is an evaluate error.
                            step.ExecutionContext.Error(conditionEvaluateError);
                            CompleteStep(step, nextStep, TaskResult.Failed);
                        }
                        else
                        {
                            // Run the step.
                            await RunStepAsync(step, jobContext.CancellationToken);
                            CompleteStep(step, nextStep);
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
                }

                // Update the job result.
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

                Trace.Info($"Current state: job state = '{jobContext.Result}'");
            }
        }

        private async Task RunStepAsync(IStep step, CancellationToken jobCancellationToken)
        {
            // Check to see if we can expand the display name
            if (step is IActionRunner actionRunner &&
                actionRunner.Stage == ActionRunStage.Main &&
                actionRunner.TryEvaluateDisplayName(step.ExecutionContext.ExpressionValues, step.ExecutionContext))
            {
                step.ExecutionContext.UpdateTimelineRecordDisplayName(actionRunner.DisplayName);
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

#if OS_WINDOWS
            try
            {
                if (Console.InputEncoding.CodePage != 65001)
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
            }
            catch (Exception ex)
            {
                Trace.Warning($"'chcp 65001' failed with exception {ex.Message}");
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
                step.ExecutionContext.Result = TaskResultUtil.MergeTaskResults(step.ExecutionContext.Result, step.ExecutionContext.CommandResult.Value);
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

        private void CompleteStep(IStep step, IStep nextStep, TaskResult? result = null, string resultCode = null)
        {
            var executionContext = step.ExecutionContext;
            if (!string.IsNullOrEmpty(executionContext.ScopeName))
            {
                // Gather current and ancestor scopes to finalize
                var scope = executionContext.Scopes[executionContext.ScopeName];
                var scopesToFinalize = default(Queue<ContextScope>);
                var nextStepScopeName = nextStep?.ExecutionContext.ScopeName;
                while (scope != null &&
                    !string.Equals(nextStepScopeName, scope.Name, StringComparison.OrdinalIgnoreCase) &&
                    !(nextStepScopeName ?? string.Empty).StartsWith($"{scope.Name}.", StringComparison.OrdinalIgnoreCase))
                {
                    if (scopesToFinalize == null)
                    {
                        scopesToFinalize = new Queue<ContextScope>();
                    }
                    scopesToFinalize.Enqueue(scope);
                    scope = string.IsNullOrEmpty(scope.ParentName) ? null : executionContext.Scopes[scope.ParentName];
                }

                // Finalize current and ancestor scopes
                var stepsContext = step.ExecutionContext.StepsContext;

                // TODO: Remove
                if (scopesToFinalize?.Count > 0) {
                    Trace.Info("scopesToFinalize?.Count > 0");
                }
                while (scopesToFinalize?.Count > 0)
                {
                    scope = scopesToFinalize.Dequeue();
                    executionContext.Debug($"Finalizing scope '{scope.Name}'");
                    executionContext.ExpressionValues["steps"] = stepsContext.GetScope(scope.Name);
                    executionContext.ExpressionValues["inputs"] = null;
                    var templateEvaluator = executionContext.ToPipelineTemplateEvaluator();
                    var outputs = default(DictionaryContextData);
                    try
                    {
                        // TODO: Figure out how to process all the outputs from the composite action as 1 job step.
                        outputs = templateEvaluator.EvaluateStepScopeOutputs(scope.Outputs, executionContext.ExpressionValues, executionContext.ExpressionFunctions);
                    }
                    catch (Exception ex)
                    {
                        Trace.Info($"Caught exception from finalize scope '{scope.Name}'");
                        Trace.Error(ex);
                        executionContext.Error(ex);
                        executionContext.Complete(TaskResult.Failed);
                        return;
                    }

                    if (outputs?.Count > 0)
                    {
                        var parentScopeName = scope.ParentName;
                        var contextName = scope.ContextName;
                        foreach (var pair in outputs)
                        {
                            var outputName = pair.Key;
                            var outputValue = pair.Value.ToString();
                            // TODO: Figure out what scope to use for setting output for Composite Action
                            stepsContext.SetOutput(parentScopeName, contextName, outputName, outputValue, out var reference);
                            executionContext.Debug($"{reference}='{outputValue}'");
                        }
                    }
                }
            }

            executionContext.Complete(result, resultCode: resultCode);
        }

        private sealed class ConditionTraceWriter : ObjectTemplating::ITraceWriter
        {
            private readonly IExecutionContext _executionContext;
            private readonly Tracing _trace;
            private readonly StringBuilder _traceBuilder = new StringBuilder();

            public string Trace => _traceBuilder.ToString();

            public ConditionTraceWriter(Tracing trace, IExecutionContext executionContext)
            {
                ArgUtil.NotNull(trace, nameof(trace));
                _trace = trace;
                _executionContext = executionContext;
            }

            public void Error(string format, params Object[] args)
            {
                var message = StringUtil.Format(format, args);
                _trace.Error(message);
                _executionContext?.Debug(message);
            }

            public void Info(string format, params Object[] args)
            {
                var message = StringUtil.Format(format, args);
                _trace.Info(message);
                _executionContext?.Debug(message);
                _traceBuilder.AppendLine(message);
            }

            public void Verbose(string format, params Object[] args)
            {
                var message = StringUtil.Format(format, args);
                _trace.Verbose(message);
                _executionContext?.Debug(message);
            }
        }
    }
}
