using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
            String jobFactoryDisplayName)
        {
            var result = new StrategyResult();

            if (token != null)
            {
                var context = CreateContext(null);
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
                configuration.ContextData.Add(PipelineTemplateConstants.Parallel, null);
                configuration.ContextData.Add(PipelineTemplateConstants.Matrix, null);
                configuration.ContextData.Add(
                    PipelineTemplateConstants.Strategy,
                    new DictionaryContextData
                    {
                        {
                            "fail-fast",
                            new StringContextData(result.FailFast.ToString(CultureInfo.InvariantCulture).ToLowerInvariant())
                        },
                        {
                            "job-index",
                            new StringContextData("0")
                        },
                        {
                            "job-total",
                            new StringContextData("1")
                        },
                        {
                            "max-parallel",
                            new StringContextData("1")
                        }
                    });
                result.Configurations.Add(configuration);
            }

            return result;
        }

        public String EvaluateJobDisplayName(
            TemplateToken token,
            IDictionary<String, PipelineContextData> contextData,
            String defaultDisplayName)
        {
            var result = default(String);

            if (token != null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.ScalarStrategyContext, token, 0, null, omitHeader: true);
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
            IDictionary<String, PipelineContextData> contextData)
        {
            var result = default(PhaseTarget);

            if (token != null)
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
            IDictionary<String, PipelineContextData> contextData)
        {
            var result = default(Int32?);

            if (token != null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.ScalarStrategyContext, token, 0, null, omitHeader: true);
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
            IDictionary<String, PipelineContextData> contextData)
        {
            var result = default(Int32?);

            if (token != null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.ScalarStrategyContext, token, 0, null, omitHeader: true);
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

            if (token != null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.ActionsScopeInputs, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = token.ToContextData().AssertDictionary("actions scope inputs");
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

            if (token != null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.ActionsScopeOutputs, token, 0, null, omitHeader: true);
                    context.Errors.Check();
                    result = token.ToContextData().AssertDictionary("actions scope outputs");
                }
                catch (Exception ex) when (!(ex is TemplateValidationException))
                {
                    context.Errors.Add(ex);
                }

                context.Errors.Check();
            }

            return result ?? new DictionaryContextData();
        }

        public Dictionary<String, String> EvaluateStepEnvironment(
            TemplateToken token,
            IDictionary<String, PipelineContextData> contextData,
            StringComparer keyComparer)
        {
            var result = default(Dictionary<String, String>);

            if (token != null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.ActionEnv, token, 0, null, omitHeader: true);
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

            if (token != null)
            {
                var context = CreateContext(contextData);
                try
                {
                    token = TemplateEvaluator.Evaluate(context, PipelineTemplateConstants.ActionWith, token, 0, null, omitHeader: true);
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

        private TemplateContext CreateContext(IDictionary<String, PipelineContextData> contextData)
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

            if (contextData?.Count > 0)
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
            "github", // todo: move to const
            PipelineTemplateConstants.Strategy,
            PipelineTemplateConstants.Matrix,
            PipelineTemplateConstants.Parallel,
            PipelineTemplateConstants.Secrets,
            PipelineTemplateConstants.Actions,
            PipelineTemplateConstants.Inputs,
        };
    }
}
