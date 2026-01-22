#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;
using System.Threading;
using GitHub.Actions.Expressions;
using GitHub.Actions.Expressions.Data;
using GitHub.Actions.Expressions.Sdk.Functions;
using GitHub.Actions.WorkflowParser.Conversion;
using GitHub.Actions.WorkflowParser.ObjectTemplating;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Schema;
using GitHub.Actions.WorkflowParser.ObjectTemplating.Tokens;
using ITraceWriter = GitHub.Actions.WorkflowParser.ObjectTemplating.ITraceWriter;

namespace GitHub.Actions.WorkflowParser
{
    /// <summary>
    /// Evaluates parts of the workflow DOM. For example, a job strategy or step inputs.
    /// </summary>
    public class WorkflowTemplateEvaluator
    {
        /// <summary>
        /// Creates a new instance for evaluating tokens within a workflow template.
        /// </summary>
        /// <param name="trace">Optional trace writer for telemetry</param>
        /// <param name="fileTable">Optional file table from the workflow template, for better error messages</param>
        /// <param name="features">Optional workflow features</param>
        public WorkflowTemplateEvaluator(
            ITraceWriter trace,
            IList<String> fileTable,
            WorkflowFeatures features)
        {
            m_trace = trace ?? new EmptyTraceWriter();
            m_fileTable = fileTable;
            m_features = features ?? WorkflowFeatures.GetDefaults();
            m_schema = WorkflowSchemaFactory.GetSchema(m_features);
        }

        /// <summary>
        /// Creates a new instance for evaluating tokens within a workflow template.
        /// </summary>
        /// <param name="trace">Optional trace writer for telemetry</param>
        /// <param name="fileTable">Optional file table from the workflow template, for better error messages</param>
        /// <param name="features">Optional workflow features</param>
        /// <param name="parentMemory">Optional parent memory counter, for byte tracking across evaluation calls.</param>
        public WorkflowTemplateEvaluator(
            ITraceWriter trace,
            IList<String> fileTable,
            WorkflowFeatures features,
            TemplateMemory parentMemory)
        {
            m_trace = trace ?? new EmptyTraceWriter();
            m_fileTable = fileTable;
            m_features = features ?? WorkflowFeatures.GetDefaults();
            m_schema = WorkflowSchemaFactory.GetSchema(m_features);
            m_parentMemory = parentMemory;
        }

        public Int32 MaxDepth => 50;

        /// <summary>
        /// Gets the maximum error message length before the message will be truncated.
        /// </summary>
        public Int32 MaxErrorMessageLength { get; set; } = 500;

        /// <summary>
        /// Gets the maximum number of errors that can be recorded when parsing a workflow.
        /// </summary>
        public Int32 MaxErrors => 10;

        public Int32 MaxEvents => 1000000; // 1 million

        public Int32 MaxResultSize { get; set; } = 10 * 1024 * 1024; // 10 mb

        public Boolean EvaluateStageIf(
            String stageId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<String, Object>> expressionState)
        {
            var result = default(Boolean?);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.If}' for stage '{stageId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions, expressionState);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.JobIfResult, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToIfResult(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result ?? throw new InvalidOperationException("Stage if cannot be null");
        }

        public Boolean EvaluateJobIf(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<String, Object>> expressionState)
        {
            var result = default(Boolean?);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.If}' for job '{jobId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions, expressionState);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.JobIfResult, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToIfResult(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result ?? throw new InvalidOperationException("Job if cannot be null");
        }

        /// <summary>
        /// Evaluates a job strategy token
        /// </summary>
        /// <param name="jobName">The default job display name (any display name expression is evaluated after strategy)</param>
        public Strategy EvaluateStrategy(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions,
            String jobName)
        {
            var result = new Strategy();
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.Strategy}' for job '{jobId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.Strategy, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToStrategy(context, token, jobName);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            if (result.Configurations.Count == 0)
            {
                var configuration = new StrategyConfiguration
                {
                    Id = WorkflowConstants.DefaultJobName,
                    Name = new JobNameBuilder(jobName).Build(),
                };
                configuration.ExpressionData.Add(WorkflowTemplateConstants.Matrix, null);
                configuration.ExpressionData.Add(
                    WorkflowTemplateConstants.Strategy,
                    new DictionaryExpressionData
                    {
                        {
                            "fail-fast",
                            new BooleanExpressionData(result.FailFast)
                        },
                        {
                            "job-index",
                            new NumberExpressionData(0)
                        },
                        {
                            "job-total",
                            new NumberExpressionData(1)
                        },
                        {
                            "max-parallel",
                            new NumberExpressionData(1)
                        }
                    });
                result.Configurations.Add(configuration);
            }

            return result;
        }

        public String EvaluateJobName(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions,
            String defaultName)
        {
            var result = default(String);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.Name}' for job '{jobId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.StringStrategyContext, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToJobName(context, token);
                    if (string.IsNullOrEmpty(result))
                    {
                        result = defaultName;
                        context.Memory.AddBytes(defaultName);
                    }
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result;
        }

        public DictionaryExpressionData EvaluateWorkflowJobInputs(
            ReusableWorkflowJob workflowJob,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var inputDefinitions = workflowJob.InputDefinitions;
            var inputValues = workflowJob.InputValues;
            var result = default(DictionaryExpressionData);

            if (inputDefinitions != null && inputDefinitions.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    var inputDefinitionsToken = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.WorkflowCallInputs, inputDefinitions, 0, null);
                    context.Errors.Check();
                    var inputValuesToken = default(TemplateToken);
                    if (inputValues != null && inputValues.Type != TokenType.Null)
                    {
                        inputValuesToken = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.WorkflowJobWith, inputValues, 0, null);
                        context.Errors.Check();
                    }
                    result = WorkflowTemplateConverter.ConvertToWorkflowJobInputs(context, inputDefinitionsToken, inputValuesToken, workflowJob);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new DictionaryExpressionData();
        }

        public IDictionary<String, String> EvaluateWorkflowJobOutputs(
            MappingToken outputDefinitions,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(IDictionary<String, String>);

            if (outputDefinitions != null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    var outputs = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.WorkflowCallOutputs, outputDefinitions, 0, null);

                    result = WorkflowTemplateConverter.ConvertToWorkflowJobOutputs(outputs);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        }

        public ActionsEnvironmentReference EvaluateJobEnvironment(
            string jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(ActionsEnvironmentReference);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.Environment}' for job '{jobId}'.";
            if (token != null && token.Type != TokenType.Null)
            {
                // Set "addMissingContexts:false" because the environment contains some properties
                // that are intended to be evaluated on the server, and others on the runner.
                //
                // For example:
                //    environment:
                //      name: ${{ this evaluates on the server }}
                //      url: ${{ this evaluates on the runner }}
                var context = CreateContext(expressionData, expressionFunctions, addMissingContexts: false);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.JobEnvironment, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToActionEnvironmentReference(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result;
        }

        public TemplateToken EvaluateJobEnvironmentUrl(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(TemplateToken);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.StringRunnerContextNoSecrets, token, 0, null);
                    context.Errors.Check();
                    result = token.AssertString("environment.url");
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public GroupPermitSetting EvaluateConcurrency(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(GroupPermitSetting);
            string type;
            string errorPrefix;
            if (String.IsNullOrEmpty(jobId))
            {
                type = WorkflowTemplateConstants.WorkflowConcurrency;
                errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.Concurrency}'.";
            }
            else
            {
                type = WorkflowTemplateConstants.JobConcurrency;
                errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.Concurrency}' for job '{jobId}'.";
            }

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, type, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToConcurrency(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result;
        }

        public RunsOn EvaluateRunsOn(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(RunsOn);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.RunsOn}' for job '{jobId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.RunsOn, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToRunsOn(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result ?? throw new InvalidOperationException("Job target cannot be null");
        }

        public Snapshot EvaluateSnapshot(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Snapshot);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.Snapshot}' for job '{jobId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.Snapshot, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToSnapshot(context, token);
                }
                catch (Exception ex) when (ex is not TemplateValidationException)
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result;
        }

        public Int32 EvaluateJobTimeout(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Int32?);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.TimeoutMinutes}' for job '{jobId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.NumberStrategyContext, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToJobTimeout(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result ?? WorkflowConstants.DefaultJobTimeoutInMinutes;
        }

        public Int32 EvaluateJobCancelTimeout(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Int32?);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.CancelTimeoutMinutes}' for job '{jobId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.NumberStrategyContext, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToJobCancelTimeout(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result ?? WorkflowConstants.DefaultJobCancelTimeoutInMinutes;
        }

        public Boolean EvaluateJobContinueOnError(
            String jobId,
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Boolean?);
            var errorPrefix = $"Error when evaluating '{WorkflowTemplateConstants.ContinueOnError}' for job '{jobId}'.";

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.BooleanStrategyContext, token, 0, null);
                    context.Errors.Check(errorPrefix);
                    result = WorkflowTemplateConverter.ConvertToJobContinueOnError(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check(errorPrefix);
            }

            return result ?? false;
        }

        public Boolean EvaluateStepContinueOnError(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Boolean?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.BooleanStepsContext, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToStepContinueOnError(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? false;
        }

        public String EvaluateStepName(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(String);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.StringStepsContext, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToStepName(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public Boolean EvaluateStepIf(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<String, Object>> expressionState)
        {
            var result = default(Boolean?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions, expressionState);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.StepIfResult, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToIfResult(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? throw new InvalidOperationException("Step if cannot be null");
        }

        public Dictionary<String, String> EvaluateStepEnvironment(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions,
            StringComparer keyComparer)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.StepEnv, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToStepEnvironment(context, token, keyComparer);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new Dictionary<String, String>(keyComparer);
        }

        public Dictionary<String, String> EvaluateStepInputs(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.StepWith, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToStepInputs(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        }

        public Int32 EvaluateStepTimeout(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Int32?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.NumberStepsContext, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToStepTimeout(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? 0;
        }

        public JobContainer EvaluateJobContainer(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(JobContainer);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.Container, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToJobContainer(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public IList<KeyValuePair<String, JobContainer>> EvaluateJobServiceContainers(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(List<KeyValuePair<String, JobContainer>>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.Services, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToJobServiceContainers(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }


        public Boolean? EvaluateJobEnvironmentDeployment(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            Boolean? result = null;

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.StringRunnerContextNoSecrets, token, 0, null);
                    context.Errors.Check();
                    var boolToken = token.AssertBoolean("environment.deployment");
                    result = boolToken.Value;
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public Dictionary<String, String> EvaluateJobDefaultsRun(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.JobDefaultsRun, token, 0, null);
                    context.Errors.Check();
                    result = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                    var mapping = token.AssertMapping("defaults run");
                    foreach (var pair in mapping)
                    {
                        // Literal key
                        var key = pair.Key.AssertString("defaults run key");

                        // Literal value
                        var value = pair.Value.AssertString("defaults run value");
                        result[key.Value] = value.Value;
                    }
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public Dictionary<String, String> EvaluateJobOutputs(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.JobOutputs, token, 0, null);
                    context.Errors.Check();
                    result = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                    var mapping = token.AssertMapping("outputs");
                    foreach (var pair in mapping)
                    {
                        // Literal key
                        var key = pair.Key.AssertString("output key");

                        // Literal value
                        var value = pair.Value.AssertString("output value");
                        result[key.Value] = value.Value;
                    }
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public IDictionary<String, String> EvaluateWorkflowJobSecrets(
            TemplateToken token,
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(IDictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(expressionData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, WorkflowTemplateConstants.WorkflowJobSecrets, token, 0, null);
                    context.Errors.Check();
                    result = WorkflowTemplateConverter.ConvertToWorkflowJobSecrets(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
        }

        private TemplateContext CreateContext(
            DictionaryExpressionData expressionData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<String, Object>> expressionState = null,
            Boolean addMissingContexts = true)
        {
            var result = new TemplateContext
            {
                CancellationToken = CancellationToken.None,
                Errors = new TemplateValidationErrors(MaxErrors, MaxErrorMessageLength),
                Memory = new TemplateMemory(
                    maxDepth: MaxDepth,
                    maxEvents: MaxEvents,
                    maxBytes: MaxResultSize,
                    parent: m_parentMemory),
                Schema = m_schema,
                StrictJsonParsing = m_features.StrictJsonParsing,
                TraceWriter = m_trace,
            };
            result.SetFeatures(m_features);

            // Add the file table
            if (m_fileTable?.Count > 0)
            {
                foreach (var file in m_fileTable)
                {
                    result.GetFileId(file);
                }
            }

            // Add named values
            if (expressionData != null)
            {
                foreach (var pair in expressionData)
                {
                    result.ExpressionValues[pair.Key] = pair.Value;
                }
            }

            // Add functions
            var functionNames = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
            if (expressionFunctions?.Count > 0)
            {
                foreach (var function in expressionFunctions)
                {
                    result.ExpressionFunctions.Add(function);
                    functionNames.Add(function.Name);
                }
            }

            // Add missing expression values and expression functions.
            // This solves the following problems:
            //   - Compat for new agent against old server (new contexts not sent down in job message)
            //   - Evaluating early when all referenced contexts are available, even though all allowed
            //     contexts may not yet be available. For example, evaluating step name can often
            //     be performed early.
            if (addMissingContexts)
            {
                foreach (var name in s_expressionValueNames)
                {
                    if (!result.ExpressionValues.ContainsKey(name))
                    {
                        result.ExpressionValues[name] = null;
                    }
                }
                foreach (var name in s_expressionFunctionNames)
                {
                    if (!functionNames.Contains(name))
                    {
                        result.ExpressionFunctions.Add(new FunctionInfo<NoOperation>(name, 0, Int32.MaxValue));
                    }
                }
            }

            // Add vars context even if addMissingContexts is false to avoid
            // JobEnvironment Evaluation errors
            if(!result.ExpressionValues.ContainsKey(WorkflowTemplateConstants.Vars))
            {
                result.ExpressionValues[WorkflowTemplateConstants.Vars] = null;
            }

            // Add state
            if (expressionState != null)
            {
                foreach (var pair in expressionState)
                {
                    result.State[pair.Key] = pair.Value;
                }
            }

            return result;
        }

        private readonly ITraceWriter m_trace;
        private readonly TemplateSchema m_schema;
        private readonly IList<String> m_fileTable;
        private readonly WorkflowFeatures m_features;
        private readonly TemplateMemory m_parentMemory;
        private readonly String[] s_expressionValueNames = new[]
        {
            WorkflowTemplateConstants.GitHub,
            WorkflowTemplateConstants.Needs,
            WorkflowTemplateConstants.Strategy,
            WorkflowTemplateConstants.Matrix,
            WorkflowTemplateConstants.Secrets,
            WorkflowTemplateConstants.Vars,
            WorkflowTemplateConstants.Steps,
            WorkflowTemplateConstants.Inputs,
            WorkflowTemplateConstants.Jobs,
            WorkflowTemplateConstants.Job,
            WorkflowTemplateConstants.Runner,
            WorkflowTemplateConstants.Env,
        };
        private readonly String[] s_expressionFunctionNames = new[]
        {
            WorkflowTemplateConstants.Always,
            WorkflowTemplateConstants.Cancelled,
            WorkflowTemplateConstants.Failure,
            WorkflowTemplateConstants.HashFiles,
            WorkflowTemplateConstants.Success,
        };
    }
}
