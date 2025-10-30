#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using System.Collections.Generic;

namespace GitHub.Actions.Expressions.Sdk
{
    public sealed class EvaluationContext
    {
        internal EvaluationContext(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options,
            ExpressionNode node)
        {
            Trace = trace ?? throw new ArgumentNullException(nameof(trace));
            SecretMasker = secretMasker ?? throw new ArgumentNullException(nameof(secretMasker));
            State = state;

            // Copy the options
            options = new EvaluationOptions(copy: options);
            if (options.MaxMemory == 0)
            {
                // Set a reasonable default max memory
                options.MaxMemory = 1048576; // 1 mb
            }
            if (options.MaxCacheMemory <= 0)
            {
                // Set a reasonable default max cache bytes
                options.MaxCacheMemory = 1048576; // 1 mb
            }
            Options = options;
            Memory = new EvaluationMemory(options.MaxMemory, node);

            m_traceResults = new Dictionary<ExpressionNode, String>();
            m_traceMemory = new MemoryCounter(null, options.MaxCacheMemory);
        }

        public ITraceWriter Trace { get; }

        public ISecretMasker SecretMasker { get; }

        public Object State { get; }

        internal EvaluationMemory Memory { get; }

        internal EvaluationOptions Options { get; }

        internal void SetTraceResult(
            ExpressionNode node,
            EvaluationResult result)
        {
            // Remove if previously added. This typically should not happen. This could happen
            // due to a badly authored function. So we'll handle it and track memory correctly.
            if (m_traceResults.TryGetValue(node, out String oldValue))
            {
                m_traceMemory.Remove(oldValue);
                m_traceResults.Remove(node);
            }

            // Check max memory
            String value = ExpressionUtility.FormatValue(SecretMasker, result);
            if (m_traceMemory.TryAdd(value))
            {
                // Store the result
                m_traceResults[node] = value;
            }
        }

        internal Boolean TryGetTraceResult(ExpressionNode node, out String value)
        {
            return m_traceResults.TryGetValue(node, out value);
        }

        private readonly Dictionary<ExpressionNode, String> m_traceResults = new Dictionary<ExpressionNode, String>();
        private readonly MemoryCounter m_traceMemory;
    }
}
