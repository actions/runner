using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk.Functions;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using ExpressionConstants = GitHub.DistributedTask.Expressions2.ExpressionConstants;
using ITraceWriter = GitHub.DistributedTask.ObjectTemplating.ITraceWriter;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    /// <summary>
    /// Evaluates parts of the workflow DOM. For example, a job strategy or step inputs.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineTemplateEvaluator
    {
        public PipelineTemplateEvaluator(
            ITraceWriter trace,
            TemplateSchema schema,
            IList<String> fileTable)
        {
            if (!String.Equals(schema.Version, PipelineTemplateConstants.Workflow_1_0, StringComparison.Ordinal))
            {
                throw new NotSupportedException($"Unexpected template schema version '{schema.Version}'");
            }

            m_trace = trace;
            m_schema = schema;
            m_fileTable = fileTable;
        }

        public Int32 MaxDepth => 50;

        /// <summary>
        /// Gets the maximum error message length before the message will be truncated.
        /// </summary>
        public Int32 MaxErrorMessageLength => 500;

        /// <summary>
        /// Gets the maximum number of errors that can be recorded when parsing a pipeline.
        /// </summary>
        public Int32 MaxErrors => 10;

        public Int32 MaxEvents => 1000000; // 1 million

        public Int32 MaxResultSize { get; set; } = 10 * 1024 * 1024; // 10 mb

        public Boolean EvaluateStepContinueOnError(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Boolean?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.BooleanStepsContext, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToStepContinueOnError(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? false;
        }

        public String EvaluateStepDisplayName(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(String);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.StringStepsContext, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToStepDisplayName(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public Dictionary<String, String> EvaluateStepEnvironment(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            StringComparer keyComparer)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.StepEnv, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToStepEnvironment(context, token, keyComparer);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new Dictionary<String, String>(keyComparer);
        }

        public Boolean EvaluateStepIf(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<String, Object>> expressionState)
        {
            var result = default(Boolean?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions, expressionState);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.StepIfResult, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToIfResult(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? throw new InvalidOperationException("Step if cannot be null");
        }

        public Dictionary<String, String> EvaluateStepInputs(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.StepWith, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToStepInputs(context, token);
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
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Int32?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.NumberStepsContext, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToStepTimeout(context, token);
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
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(JobContainer);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.Container, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToJobContainer(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        public Dictionary<String, String> EvaluateJobOutput(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.JobOutputs, token, 0, null, omitHeader: true);
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

        public TemplateToken EvaluateEnvironmentUrl(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(TemplateToken);
            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, TemplateConstants.StringRunnerContextNoSecrets, token, 0, null, omitHeader: true);
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


        public Dictionary<String, String> EvaluateJobDefaultsRun(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.JobDefaultsRun, token, 0, null, omitHeader: true);
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

        public IList<KeyValuePair<String, JobContainer>> EvaluateJobServiceContainers(
            TemplateToken token,
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions)
        {
            var result = default(List<KeyValuePair<String, JobContainer>>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData, expressionFunctions);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.Services, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToJobServiceContainers(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result;
        }

        private TemplateContext CreateContext(
            DictionaryContextData contextData,
            IList<IFunctionInfo> expressionFunctions,
            IEnumerable<KeyValuePair<String, Object>> expressionState = null)
        {
            var result = new TemplateContext
            {
                CancellationToken = CancellationToken.None,
                Errors = new TemplateValidationErrors(MaxErrors, MaxErrorMessageLength),
                Memory = new TemplateMemory(
                    maxDepth: MaxDepth,
                    maxEvents: MaxEvents,
                    maxBytes: MaxResultSize),
                Schema = m_schema,
                TraceWriter = m_trace,
            };

            // Add the file table
            if (m_fileTable?.Count > 0)
            {
                foreach (var file in m_fileTable)
                {
                    result.GetFileId(file);
                }
            }

            // Add named values
            if (contextData != null)
            {
                foreach (var pair in contextData)
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
            //     contexts may not yet be available. For example, evaluating step display name can often
            //     be performed early.
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
        private readonly String[] s_expressionValueNames = new[]
        {
            PipelineTemplateConstants.GitHub,
            PipelineTemplateConstants.Needs,
            PipelineTemplateConstants.Strategy,
            PipelineTemplateConstants.Matrix,
            PipelineTemplateConstants.Needs,
            PipelineTemplateConstants.Secrets,
            PipelineTemplateConstants.Steps,
            PipelineTemplateConstants.Inputs,
            PipelineTemplateConstants.Job,
            PipelineTemplateConstants.Runner,
            PipelineTemplateConstants.Env,
        };
        private readonly String[] s_expressionFunctionNames = new[]
        {
            PipelineTemplateConstants.Always,
            PipelineTemplateConstants.Cancelled,
            PipelineTemplateConstants.Failure,
            PipelineTemplateConstants.HashFiles,
            PipelineTemplateConstants.Success,
        };
    }
}
