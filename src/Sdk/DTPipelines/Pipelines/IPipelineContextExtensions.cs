using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class IPipelineContextExtensions
    {
        /// <summary>
        /// Uses the current context to validate the steps provided.
        /// </summary>
        /// <param name="context">The current pipeline context</param>
        /// <param name="steps">The list of steps which should be validated</param>
        /// <param name="options">The options controlling the level of validation performed</param>
        /// <returns>A list of validation errors which were encountered, if any</returns>
        public static IList<PipelineValidationError> Validate(
            this IPipelineContext context,
            IList<Step> steps,
            PhaseTarget target,
            BuildOptions options)
        {
            var builder = new PipelineBuilder(context);
            return builder.Validate(steps, target, options);
        }

        /// <summary>
        /// Evaluates a property which is specified as an expression and writes the resulting value to the 
        /// corresponding trace log if one is specified on the context.
        /// </summary>
        /// <typeparam name="T">The result type of the expression</typeparam>
        /// <param name="context">The pipeline context</param>
        /// <param name="name">The name of the property being evaluated</param>
        /// <param name="expression">The expression which should be evaluated</param>
        /// <param name="defaultValue">The default value if no expression is specified</param>
        /// <param name="traceDefault">True to write the default value if no expression is specified; otherwise, false</param>
        /// <returns>The result of the expression evaluation</returns>
        internal static ExpressionResult<T> Evaluate<T>(
            this IPipelineContext context,
            String name,
            ExpressionValue<T> expression,
            T defaultValue,
            Boolean traceDefault = true)
        {
            ExpressionResult<T> result = null;
            if (expression != null)
            {
                if (expression.IsLiteral)
                {
                    context.Trace?.Info($"{name}: {GetTraceValue(expression.Literal)}");
                    result = new ExpressionResult<T>(expression.Literal);
                }
                else
                {
                    context.Trace?.EnterProperty(name);
                    result = expression.GetValue(context);
                    context.Trace?.LeaveProperty(name);
                }
            }
            else if (traceDefault && context.Trace != null)
            {
                context.Trace.Info($"{name}: {defaultValue}");
            }

            return result ?? new ExpressionResult<T>(defaultValue);
        }

        private static String GetTraceValue<T>(T value)
        {
            if (value.GetType().IsValueType)
            {
                return value.ToString();
            }
            else
            {
                return $"{System.Environment.NewLine}{JsonUtility.ToString(value, indent: true)}";
            }
        }
    }
}
