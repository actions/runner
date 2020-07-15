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
    [ServiceLocator(Default = typeof(CompositeStepsRunner))]
    public interface ICompositeStepsRunner : IRunnerService
    {
        Task RunAsync(IExecutionContext Context);
    }


    public sealed class CompositeStepsRunner : RunnerService, ICompositeStepsRunner
    {
        public async Task RunAsync(IExecutionContext actionContext)
        {
            // Another approach we can explore, is moving all this logic to the CompositeActionHandler if it's small enough. 

            ArgUtil.NotNull(actionContext, nameof(actionContext));
            ArgUtil.NotNull(actionContext.CompositeSteps, nameof(actionContext.CompositeSteps));

            // The parent StepsRunner of the whole Composite Action Step handles the cancellation stuff already. 
            while (actionContext.CompositeSteps.Count > 0)
            {
                // This is used for testing UI appearance.
                // System.Threading.Thread.Sleep(5000);

                var step = actionContext.CompositeSteps[0];
                actionContext.CompositeSteps.RemoveAt(0);

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
                        Trace.Info("Caught exception in Composite Steps Runner from expression for step.env");
                        // evaluateStepEnvFailed = true;
                        step.ExecutionContext.Error(ex);
                        // CompleteStep(step, TaskResult.Failed);
                    }
                }

                // We don't have to worry about the cancellation token stuff because that's handled by the composite action level (in the StepsRunner)

                await RunStepAsync(step);

                // TODO: Add compat for other types of steps.
            }
            // Completion Status handled by StepsRunner for the whole Composite Action Step
        }

        private async Task RunStepAsync(IStep step)
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
    }
}