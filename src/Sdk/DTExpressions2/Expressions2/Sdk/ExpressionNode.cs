using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.Logging;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ExpressionNode : IExpressionNode
    {
        internal Container Container { get; set; }

        internal Int32 Level { get; private set; }

        /// <summary>
        /// The name is used for tracing. Normally the parser will set the name. However if a node
        /// is added manually, then the name may not be set and will fallback to the type name.
        /// </summary>
        protected internal String Name
        {
            get
            {
                return !String.IsNullOrEmpty(m_name) ? m_name : this.GetType().Name;
            }

            set
            {
                m_name = value;
            }
        }

        /// <summary>
        /// Indicates whether the evalation result should be stored on the context and used
        /// when the realized result is traced.
        /// </summary>
        protected abstract Boolean TraceFullyRealized { get; }

        /// <summary>
        /// IExpressionNode entry point.
        /// </summary>
        EvaluationResult IExpressionNode.Evaluate(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options)
        {
            if (Container != null)
            {
                // Do not localize. This is an SDK consumer error.
                throw new NotSupportedException($"Expected {nameof(IExpressionNode)}.{nameof(Evaluate)} to be called on root node only.");
            }


            var originalSecretMasker = secretMasker;
            try
            {
                // Evaluate
                secretMasker = secretMasker?.Clone() ?? new SecretMasker();
                trace = new EvaluationTraceWriter(trace, secretMasker);
                var context = new EvaluationContext(trace, secretMasker, state, options, this);
                trace.Info($"Evaluating: {ConvertToExpression()}");
                var result = Evaluate(context);

                // Trace the result
                TraceTreeResult(context, result.Value, result.Kind);

                return result;
            }
            finally
            {
                if (secretMasker != null && secretMasker != originalSecretMasker)
                {
                    (secretMasker as IDisposable)?.Dispose();
                    secretMasker = null;
                }
            }
        }

        /// <summary>
        /// This function is intended only for ExpressionNode authors to call. The EvaluationContext
        /// caches result-state specific to the evaluation instance.
        /// </summary>
        public EvaluationResult Evaluate(EvaluationContext context)
        {
            // Evaluate
            Level = Container == null ? 0 : Container.Level + 1;
            TraceVerbose(context, Level, $"Evaluating {Name}:");
            var coreResult = EvaluateCore(context, out ResultMemory coreMemory);

            if (coreMemory == null)
            {
                coreMemory = new ResultMemory();
            }

            // Convert to canonical value
            var val = ExpressionUtility.ConvertToCanonicalValue(coreResult, out ValueKind kind, out Object raw);

            // The depth can be safely trimmed when the total size of the core result is known,
            // or when the total size of the core result can easily be determined.
            var trimDepth = coreMemory.IsTotal || (Object.ReferenceEquals(raw, null) && ExpressionUtility.IsPrimitive(kind));

            // Account for the memory overhead of the core result
            var coreBytes = coreMemory.Bytes ?? EvaluationMemory.CalculateBytes(raw ?? val);
            context.Memory.AddAmount(Level, coreBytes, trimDepth);

            // Account for the memory overhead of the conversion result
            if (!Object.ReferenceEquals(raw, null))
            {
                var conversionBytes = EvaluationMemory.CalculateBytes(val);
                context.Memory.AddAmount(Level, conversionBytes);
            }

            var result = new EvaluationResult(context, Level, val, kind, raw);

            // Store the trace result
            if (this.TraceFullyRealized)
            {
                context.SetTraceResult(this, result);
            }

            return result;
        }

        internal abstract String ConvertToExpression();

        internal abstract String ConvertToRealizedExpression(EvaluationContext context);

        /// <summary>
        /// Evaluates the node
        /// </summary>
        /// <param name="context">The current expression context</param>
        /// <param name="resultMemory">
        /// Helps determine how much memory is being consumed across the evaluation of the expression.
        /// </param>
        protected abstract Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory);

        protected MemoryCounter CreateMemoryCounter(EvaluationContext context)
        {
            return new MemoryCounter(this, context.Options.MaxMemory);
        }

        private void TraceTreeResult(
            EvaluationContext context,
            Object result,
            ValueKind kind)
        {
            // Get the realized expression
            String realizedExpression = ConvertToRealizedExpression(context);

            // Format the result
            String traceValue = ExpressionUtility.FormatValue(context.SecretMasker, result, kind);

            // Only trace the realized expression if it is meaningfully different
            if (!String.Equals(realizedExpression, traceValue, StringComparison.Ordinal))
            {
                if (kind == ValueKind.Number &&
                    String.Equals(realizedExpression, $"'{traceValue}'", StringComparison.Ordinal))
                {
                    // Don't bother tracing the realized expression when the result is a number and the
                    // realized expresion is a precisely matching string.
                }
                else
                {
                    context.Trace.Info($"Expanded: {realizedExpression}");
                }
            }

            // Always trace the result
            context.Trace.Info($"Result: {traceValue}");
        }

        private static void TraceVerbose(
            EvaluationContext context,
            Int32 level,
            String message)
        {
            context.Trace.Verbose(String.Empty.PadLeft(level * 2, '.') + (message ?? String.Empty));
        }

        private static readonly ValueKind[] s_simpleKinds = new[]
        {
            ValueKind.Boolean,
            ValueKind.Null,
            ValueKind.Number,
            ValueKind.String,
        };

        private String m_name;
    }
}
