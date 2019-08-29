using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.ObjectTemplating.Schema;
using GitHub.DistributedTask.ObjectTemplating.Tokens;
using GitHub.DistributedTask.Pipelines.ContextData;

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

        public StrategyResult EvaluateStrategy(
            TemplateToken token,
            DictionaryContextData contextData,
            String jobFactoryDisplayName)
        {
            var result = new StrategyResult();

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.Strategy, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToStrategy(context, token, jobFactoryDisplayName);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            if (result.Configurations.Count == 0)
            {
                var configuration = new StrategyConfiguration
                {
                    Name = PipelineConstants.DefaultJobName,
                    DisplayName = new JobDisplayNameBuilder(jobFactoryDisplayName).Build(),
                };
                configuration.ContextData.Add(PipelineTemplateConstants.Matrix, null);
                configuration.ContextData.Add(
                    PipelineTemplateConstants.Strategy,
                    new DictionaryContextData
                    {
                        {
                            "fail-fast",
                            new BooleanContextData(result.FailFast)
                        },
                        {
                            "job-index",
                            new NumberContextData(0)
                        },
                        {
                            "job-total",
                            new NumberContextData(1)
                        },
                        {
                            "max-parallel",
                            new NumberContextData(1)
                        }
                    });
                result.Configurations.Add(configuration);
            }

            return result;
        }

        public String EvaluateJobDisplayName(
            TemplateToken token,
            DictionaryContextData contextData,
            String defaultDisplayName)
        {
            var result = default(String);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.StringStrategyContext, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToJobDisplayName(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return !String.IsNullOrEmpty(result) ? result : defaultDisplayName;
        }

        public PhaseTarget EvaluateJobTarget(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(PhaseTarget);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.RunsOn, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToJobTarget(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? throw new InvalidOperationException("Job target cannot be null");
        }

        public Int32 EvaluateJobTimeout(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(Int32?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.NumberStrategyContext, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToJobTimeout(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? PipelineConstants.DefaultJobTimeoutInMinutes;
        }

        public Int32 EvaluateJobCancelTimeout(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(Int32?);

            if (token != null && token.Type != TokenType.Null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.NumberStrategyContext, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = PipelineTemplateConverter.ConvertToJobCancelTimeout(context, token);
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? PipelineConstants.DefaultJobCancelTimeoutInMinutes;
        }

        public DictionaryContextData EvaluateStepScopeInputs(
            TemplateToken token,
            IDictionary<String, PipelineContextData> contextData)
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
            IDictionary<String, PipelineContextData> contextData)
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
            IDictionary<String, PipelineContextData> contextData)
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


        public String EvaluateStepDisplayName(
            TemplateToken token,
            IDictionary<String, PipelineContextData> contextData)
        {
            var result = String.Empty;

            switch (token)
            {
                case SequenceToken sequenceToken:
                    foreach (TemplateToken templateToken in sequenceToken)
                    {
                        var tokenString = EvaluateStepDisplayNamePart(templateToken, contextData);
                        // Abort if we recieve an empty string, so we don't end up with something like "Run "
                        if (String.IsNullOrEmpty(tokenString))
                        {
                            return String.Empty;
                        }
                        else
                        {
                            
                            result = String.IsNullOrEmpty(result) ? tokenString : String.Concat(result, tokenString);
                        }
                    }
                    break;
                default:
                    result = EvaluateStepDisplayNamePart(token, contextData);
                    break;
            }
            return String.IsNullOrEmpty(result) ? String.Empty : result;
        }

        public Dictionary<String, String> EvaluateStepEnvironment(
            TemplateToken token,
            IDictionary<String, PipelineContextData> contextData,
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
            IDictionary<String, PipelineContextData> contextData)
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
            IDictionary<String, PipelineContextData> contextData)
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

        public ContainerResource EvaluateJobContainer(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(ContainerResource);

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

        public Dictionary<String, ContainerResource> EvaluateJobServiceContainers(
            TemplateToken token,
            DictionaryContextData contextData)
        {
            var result = default(Dictionary<String, ContainerResource>);

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

        private TemplateContext CreateContext(IEnumerable<KeyValuePair<string, PipelineContextData>> contextData)
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

        private String EvaluateStepDisplayNamePart(TemplateToken token, IDictionary<String, PipelineContextData> contextData)
        {
            var result = default(String);
            var context = CreateContext(contextData);

            if (token != null && token.Type != TokenType.Null)
            {
                // We should only evaluate basic expressions if we are sure we have context on all the Named Values
                // Otherwise return and use a default name
                if (token is BasicExpressionToken expressionToken)
                {
                    Exception ex = null;
                    var namedValues = ExpressionToken.GetExpressionNamedValues(expressionToken.Expression, out ex);
                    if (ex != null)
                    {
                        context.Errors.Add(ex);
                        context.Errors.Check();
                    }
                    if (!namedValues.Distinct().All(value => contextData.ContainsKey(value.Name)))
                    {
                        return null;
                    }
                }

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
        };
    }
}
