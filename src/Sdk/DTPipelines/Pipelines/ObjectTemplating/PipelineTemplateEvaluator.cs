using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using GitHub.DistributedTask.Expressions2;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;
using ExpressionConstants = GitHub.DistributedTask.Expressions2.ExpressionConstants;
using ITraceWriter = GitHub.DistributedTask.ObjectTemplating.ITraceWriter;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineTemplateEvaluator
    {
        public PipelineTemplateEvaluator(
            ITraceWriter trace,
            TemplateSchema schema)
        {
            if (!String.Equals(schema.Version, PipelineTemplateConstants.Workflow_1_0, StringComparison.Ordinal))
            {
                throw new NotSupportedException($"Unexpected template schema version '{schema.Version}'");
            }

            m_trace = trace;
            m_schema = schema;
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

        public DictionaryContextData EvaluateStepScopeInputs(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(DictionaryContextData);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.StepsScopeInputs, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = token.ToContextData().AssertDictionary("steps scope inputs");
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new DictionaryContextData();
        }

        public DictionaryContextData EvaluateStepScopeOutputs(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(DictionaryContextData);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.StepsScopeOutputs, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = token.ToContextData().AssertDictionary("steps scope outputs");
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new DictionaryContextData();
        }

        public Boolean EvaluateStepContinueOnError(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(Boolean?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
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

        public Dictionary<String, String> EvaluateStepEnvironment(
            TemplateToken token,
            DictionaryContextData contextData,
            StringComparer keyComparer)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
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

        public Dictionary<String, String> EvaluateStepInputs(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(Dictionary<String, String>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
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
            DictionaryContextData contextData)
        {
            var result = default(Int32?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
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
            DictionaryContextData contextData)
        {
            var result = default(JobContainer);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
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

        public IList<KeyValuePair<String, JobContainer>> EvaluateJobServiceContainers(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(List<KeyValuePair<String, JobContainer>>);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
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

        public Boolean TryEvaluateStepDisplayName(
            TemplateToken token,
            DictionaryContextData contextData,
            out String stepName)
        {
            stepName = default(String);
            var context = CreateContext(contextData);

            if (token != null && token.Type != TokenType.Null)
            {
                // We should only evaluate basic expressions if we are sure we have context on all the Named Values and functions
                // Otherwise return and use a default name
                if (token is BasicExpressionToken expressionToken)
                {
                    ExpressionNode root = null;
                    try
                    {
                        root = new ExpressionParser().ValidateSyntax(expressionToken.Expression, null) as ExpressionNode;
                    }
                    catch (Exception exception)
                    {
                        context.Errors.Add(exception);
                        context.Errors.Check();
                    }
                    foreach (var node in root.Traverse())
                    {
                        if (node is NamedValue namedValue && !contextData.ContainsKey(namedValue.Name))
                        {
                            return false;
                        }
                        else if (node is Function function &&
                            !context.ExpressionFunctions.Any(item => String.Equals(item.Name, function.Name)) &&
                            !ExpressionConstants.WellKnownFunctions.ContainsKey(function.Name))
                        {
                            return false;
                        }
                    }
                }

                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.StringStepsContext, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    stepName = PipelineTemplateConverter.ConvertToStepDisplayName(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }
            return true;
        }

        private TemplateContext CreateContext(DictionaryContextData contextData)
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

            if (contextData != null)
            {
                foreach (var pair in contextData)
                {
                    result.ExpressionValues[pair.Key] = pair.Value;
                }
            }

            // Compat for new agent against old server
            foreach (var name in s_contextNames)
            {
                if (!result.ExpressionValues.ContainsKey(name))
                {
                    result.ExpressionValues[name] = null;
                }
            }

            return result;
        }

        private readonly ITraceWriter m_trace;
        private readonly TemplateSchema m_schema;
        private readonly String[] s_contextNames = new[]
        {
            PipelineTemplateConstants.GitHub,
            PipelineTemplateConstants.Strategy,
            PipelineTemplateConstants.Matrix,
            PipelineTemplateConstants.Secrets,
            PipelineTemplateConstants.Steps,
            PipelineTemplateConstants.Inputs,
            PipelineTemplateConstants.Job,
            PipelineTemplateConstants.Runner,
            PipelineTemplateConstants.Env,
        };
    }
}
