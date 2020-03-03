using GitHub.DistributedTask.WebApi;
using Pipelines = GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common.Util;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.Pipelines.ObjectTemplating;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

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
                        jobContext.JobSteps.Enqueue(postStep);
                    }

                    continue;
                }

                var step = jobContext.JobSteps.Dequeue();
                IStep nextStep = null;
                if (jobContext.JobSteps.Count > 0)
                {
                    nextStep = jobContext.JobSteps.Peek();
                }

                Trace.Info($"Processing step: DisplayName='{step.DisplayName}'");
                ArgUtil.NotNull(step.ExecutionContext, nameof(step.ExecutionContext));
                ArgUtil.NotNull(step.ExecutionContext.Variables, nameof(step.ExecutionContext.Variables));

                // Start
                step.ExecutionContext.Start();

                // Initialize scope
                if (InitializeScope(step, scopeInputs))
                {
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

                    if (step is IActionRunner actionStep)
                    {
                        // Set GITHUB_ACTION
                        step.ExecutionContext.SetGitHubContext("action", actionStep.Action.Name);

                        // Evaluate and merge action's env block to env context
                        var templateEvaluator = step.ExecutionContext.ToPipelineTemplateEvaluator();
                        var actionEnvironment = templateEvaluator.EvaluateStepEnvironment(actionStep.Action.Environment, step.ExecutionContext.ExpressionValues, VarUtil.EnvironmentVariableKeyComparer);
                        foreach (var env in actionEnvironment)
                        {
                            envContext[env.Key] = new StringContextData(env.Value ?? string.Empty);
                        }
                    }

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
                                jobContext.JobContext.Status = jobContext.Result?.ToActionResult();

                                step.ExecutionContext.Debug($"Re-evaluate condition on job cancellation for step: '{step.DisplayName}'.");
                                ConditionResult conditionReTestResult;
                                if (HostContext.RunnerShutdownToken.IsCancellationRequested)
                                {
                                    step.ExecutionContext.Debug($"Skip Re-evaluate condition on runner shutdown.");
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
                                jobContext.JobContext.Status = jobContext.Result?.ToActionResult();
                            }
                        }

                        // Evaluate condition.
                        step.ExecutionContext.Debug($"Evaluating condition for step: '{step.DisplayName}'");
                        Exception conditionEvaluateError = null;
                        ConditionResult conditionResult;
                        if (HostContext.RunnerShutdownToken.IsCancellationRequested)
                        {
                            step.ExecutionContext.Debug($"Skip evaluate condition on runner shutdown.");
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
                            CompleteStep(step, nextStep, TaskResult.Skipped, resultCode: conditionResult.Trace);
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
                timeoutMinutes = templateEvaluator.EvaluateStepTimeout(step.Timeout, step.ExecutionContext.ExpressionValues);
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
                    continueOnError = templateEvaluator.EvaluateStepContinueOnError(step.ContinueOnError, step.ExecutionContext.ExpressionValues);
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
                    step.ExecutionContext.Result = TaskResult.Succeeded;
                    Trace.Info($"Updated step result (continue on error)");
                }
            }
            Trace.Info($"Step result: {step.ExecutionContext.Result}");

            // Complete the step context.
            step.ExecutionContext.Debug($"Finishing: {step.DisplayName}");
        }

        private bool InitializeScope(IStep step, Dictionary<string, PipelineContextData> scopeInputs)
        {
            var executionContext = step.ExecutionContext;
            var stepsContext = executionContext.StepsContext;
            if (!string.IsNullOrEmpty(executionContext.ScopeName))
            {
                // Gather uninitialized current and ancestor scopes
                var scope = executionContext.Scopes[executionContext.ScopeName];
                var scopesToInitialize = default(Stack<ContextScope>);
                while (scope != null && !scopeInputs.ContainsKey(scope.Name))
                {
                    if (scopesToInitialize == null)
                    {
                        scopesToInitialize = new Stack<ContextScope>();
                    }
                    scopesToInitialize.Push(scope);
                    scope = string.IsNullOrEmpty(scope.ParentName) ? null : executionContext.Scopes[scope.ParentName];
                }

                // Initialize current and ancestor scopes
                while (scopesToInitialize?.Count > 0)
                {
                    scope = scopesToInitialize.Pop();
                    executionContext.Debug($"Initializing scope '{scope.Name}'");
                    executionContext.ExpressionValues["steps"] = stepsContext.GetScope(scope.ParentName);
                    executionContext.ExpressionValues["inputs"] = !String.IsNullOrEmpty(scope.ParentName) ? scopeInputs[scope.ParentName] : null;
                    var templateEvaluator = executionContext.ToPipelineTemplateEvaluator();
                    var inputs = default(DictionaryContextData);
                    try
                    {
                        inputs = templateEvaluator.EvaluateStepScopeInputs(scope.Inputs, executionContext.ExpressionValues);
                    }
                    catch (Exception ex)
                    {
                        Trace.Info($"Caught exception from initialize scope '{scope.Name}'");
                        Trace.Error(ex);
                        executionContext.Error(ex);
                        executionContext.Complete(TaskResult.Failed);
                        return false;
                    }

                    scopeInputs[scope.Name] = inputs;
                }
            }

            // Setup expression values
            var scopeName = executionContext.ScopeName;
            executionContext.ExpressionValues["steps"] = stepsContext.GetScope(scopeName);
            executionContext.ExpressionValues["inputs"] = string.IsNullOrEmpty(scopeName) ? null : scopeInputs[scopeName];

            return true;
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
                        outputs = templateEvaluator.EvaluateStepScopeOutputs(scope.Outputs, executionContext.ExpressionValues);
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
                            stepsContext.SetOutput(parentScopeName, contextName, outputName, outputValue, out var reference);
                            executionContext.Debug($"{reference}='{outputValue}'");
                        }
                    }
                }
            }

            executionContext.Complete(result, resultCode: resultCode);
        }
    }
}
