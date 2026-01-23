#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;

namespace GitHub.Actions.Expressions.Sdk
{
    public abstract class ExpressionNode : IExpressionNode
    {
        internal Container Container { get; set; }

        internal Int32 Level { get; private set; }

        /// <summary>
        /// The name is used for tracing. Normally the parser will set the name. However if a node
        /// is added manually, then the name may not be set and will fallback to the type name.
        /// </summary>
        public String Name
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
        /// Indicates whether the evaluation result should be stored on the context and used
        /// when the expanded result is traced.
        /// </summary>
        protected abstract Boolean TraceFullyExpanded { get; }

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
                secretMasker = secretMasker ?? new NoOpSecretMasker();
                trace = new EvaluationTraceWriter(trace, secretMasker);
                var context = new EvaluationContext(trace, secretMasker, state, options, this);
                var originalExpression = ConvertToExpression();
                trace.Info($"Evaluating: {originalExpression}");
                var result = Evaluate(context);

                // Trace the result
                TraceTreeResult(context, originalExpression, result.Value, result.Kind);

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
            if (this.TraceFullyExpanded)
            {
                context.SetTraceResult(this, result);
            }

            return result;
        }

        public abstract String ConvertToExpression();

        internal abstract String ConvertToExpandedExpression(EvaluationContext context);

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
            String originalExpression,
            Object result,
            ValueKind kind)
        {
            // Get the expanded expression
            String expandedExpression = ConvertToExpandedExpression(context);

            // Format the result
            String traceValue = ExpressionUtility.FormatValue(context.SecretMasker, result, kind);

            // Only trace the expanded expression if it is meaningfully different (or if always showing)
            if (context.Options.AlwaysTraceExpanded ||
                (!String.Equals(expandedExpression, originalExpression, StringComparison.Ordinal) &&
                 !String.Equals(expandedExpression, traceValue, StringComparison.Ordinal)))
            {
                if (!context.Options.AlwaysTraceExpanded &&
                    kind == ValueKind.Number &&
                    String.Equals(expandedExpression, $"'{traceValue}'", StringComparison.Ordinal))
                {
                    // Don't bother tracing the expanded expression when the result is a number and the
                    // expanded expression is a precisely matching string.
                }
                else
                {
                    context.Trace.Info($"Expanded: {expandedExpression}");
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

        private String m_name;
    }
}
