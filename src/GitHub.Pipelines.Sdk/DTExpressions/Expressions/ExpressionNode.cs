using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.TeamFoundation.DistributedTask.Logging;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class ExpressionNode : IExpressionNode
    {
        internal ContainerNode Container { get; set; }

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

        internal abstract String ConvertToExpression();

        internal abstract String ConvertToRealizedExpression(EvaluationContext context);

        /// <summary>
        /// Evaluates the node
        /// </summary>
        protected virtual Object EvaluateCore(EvaluationContext context)
        {
            throw new InvalidOperationException($"Method {nameof(EvaluateCore)} not implemented");
        }

        /// <summary>
        /// Evaluates the node
        /// </summary>
        /// <param name="context">The current expression context</param>
        /// <param name="resultMemory">
        /// Helps determine how much memory is being consumed across the evaluation of the expression.
        /// </param>
        protected virtual Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            return EvaluateCore(context);
        }

        /// <summary>
        /// INode entry point.
        /// </summary>
        public T Evaluate<T>(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options = null)
        {
            if (Container != null)
            {
                // Do not localize. This is an SDK consumer error.
                throw new NotSupportedException($"Expected {nameof(IExpressionNode)}.{nameof(Evaluate)} to be called on root node only.");
            }

            ISecretMasker originalSecretMasker = secretMasker;
            try
            {
                secretMasker = secretMasker?.Clone() ?? new SecretMasker();
                trace = new EvaluationTraceWriter(trace, secretMasker);
                var context = new EvaluationContext(trace, secretMasker, state, options, this);
                trace.Info($"Evaluating: {ConvertToExpression()}");

                // String
                if (typeof(T).Equals(typeof(String)))
                {
                    String stringResult = EvaluateString(context);
                    TraceTreeResult(context, stringResult, ValueKind.String);
                    return (T)(Object)stringResult;
                }
                // Boolean
                else if (typeof(T).Equals(typeof(Boolean)))
                {
                    Boolean booleanResult = EvaluateBoolean(context);
                    TraceTreeResult(context, booleanResult, ValueKind.Boolean);
                    return (T)(Object)booleanResult;
                }
                // Version
                else if (typeof(T).Equals(typeof(Version)))
                {
                    Version versionResult = EvaluateVersion(context);
                    TraceTreeResult(context, versionResult, ValueKind.Version);
                    return (T)(Object)versionResult;
                }
                // DateTime types
                else if (typeof(T).Equals(typeof(DateTimeOffset)))
                {
                    DateTimeOffset dateTimeResult = EvaluateDateTime(context);
                    TraceTreeResult(context, dateTimeResult, ValueKind.DateTime);
                    return (T)(Object)dateTimeResult;
                }
                else if (typeof(T).Equals(typeof(DateTime)))
                {
                    DateTimeOffset dateTimeResult = EvaluateDateTime(context);
                    TraceTreeResult(context, dateTimeResult, ValueKind.DateTime);
                    return (T)(Object)dateTimeResult.UtcDateTime;
                }

                TypeInfo typeInfo = typeof(T).GetTypeInfo();
                if (typeInfo.IsPrimitive)
                {
                    // Decimal
                    if (typeof(T).Equals(typeof(Decimal)))
                    {
                        Decimal decimalResult = EvaluateNumber(context);
                        TraceTreeResult(context, decimalResult, ValueKind.Number);
                        return (T)(Object)decimalResult;
                    }
                    // Other numeric types
                    else if (typeof(T).Equals(typeof(Byte)) ||
                        typeof(T).Equals(typeof(SByte)) ||
                        typeof(T).Equals(typeof(Int16)) ||
                        typeof(T).Equals(typeof(UInt16)) ||
                        typeof(T).Equals(typeof(Int32)) ||
                        typeof(T).Equals(typeof(UInt32)) ||
                        typeof(T).Equals(typeof(Int64)) ||
                        typeof(T).Equals(typeof(UInt64)) ||
                        typeof(T).Equals(typeof(Single)) ||
                        typeof(T).Equals(typeof(Double)))
                    {
                        Decimal decimalResult = EvaluateNumber(context);
                        trace.Verbose($"Converting expression result to type {typeof(T).Name}");
                        try
                        {
                            T numericResult = (T)Convert.ChangeType(decimalResult, typeof(T));

                            // Note, the value is converted back to decimal before tracing, in order to leverage the same
                            // util-formatting method used in other places.
                            TraceTreeResult(context, Convert.ToDecimal((Object)numericResult), ValueKind.Number);

                            return numericResult;
                        }
                        catch (Exception exception)
                        {
                            context.Trace.Verbose($"Failed to convert the result number into the type {typeof(T).Name}. {exception.Message}");
                            throw new TypeCastException(
                                secretMasker,
                                value: decimalResult,
                                fromKind: ValueKind.Number,
                                toType: typeof(T),
                                error: exception.Message);
                        }
                    }
                }

                // Generic evaluate
                EvaluationResult result = Evaluate(context);
                TraceTreeResult(context, result.Value, result.Kind);

                // JToken
                if (typeof(T).Equals(typeof(JToken)))
                {
                    if (result.Value is null)
                    {
                        return default;
                    }
                    else if (result.Value is JToken)
                    {
                        return (T)result.Value;
                    }
                    else
                    {
                        return (T)(Object)JToken.FromObject(result.Value, JsonUtility.CreateJsonSerializer());
                    }
                }
                // Object or Array
                else if (result.Kind == ValueKind.Object || result.Kind == ValueKind.Array)
                {
                    Type resultType = result.Value.GetType();
                    context.Trace.Verbose($"Result type: {resultType.Name}");
                    if (typeInfo.IsAssignableFrom(resultType.GetTypeInfo()))
                    {
                        return (T)result.Value;
                    }
                    else
                    {
                        context.Trace.Verbose($"Unable to assign result to the type {typeof(T).Name}");
                        throw new TypeCastException(fromType: resultType, toType: typeof(T));
                    }
                }
                // Null
                else if (result.Kind == ValueKind.Null)
                {
                    return default;
                }
                // String
                else if (result.Kind == ValueKind.String)
                {
                    // Treat empty string as null
                    String stringResult = result.Value as String;
                    if (String.IsNullOrEmpty(stringResult))
                    {
                        return default;
                    }

                    // Otherwise deserialize
                    try
                    {
                        return JsonUtility.FromString<T>(stringResult);
                    }
                    catch (Exception exception) when (exception is JsonReaderException || exception is JsonSerializationException)
                    {
                        context.Trace.Verbose($"Failed to json-deserialize the result string into the type {typeof(T).Name}. {exception.Message}");
                        throw new TypeCastException(
                            context.SecretMasker,
                            value: stringResult,
                            fromKind: ValueKind.String,
                            toType: typeof(T),
                            error: exception.Message);
                    }
                }
                else
                {
                    context.Trace.Verbose($"Unable to convert from kind {result.Kind} to the type {typeof(T).Name}");
                    throw new TypeCastException(
                        context.SecretMasker,
                        value: result.Value,
                        fromKind: result.Kind,
                        toType: typeof(T));
                }
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
        /// INode entry point.
        /// </summary>
        public Object Evaluate(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options = null)
        {
            return Evaluate(trace, secretMasker, state, options, out _, out _);
        }

        /// <summary>
        /// INode entry point.
        /// </summary>
        public Boolean EvaluateBoolean(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state)
        {
            return Evaluate<Boolean>(trace, secretMasker, state);
        }

        /// <summary>
        /// INode entry point.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EvaluationResult EvaluateResult(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options)
        {
            var val = Evaluate(trace, secretMasker, state, options, out ValueKind kind, out Object raw);
            return new EvaluationResult(null, 0, val, kind, raw, omitTracing: true);
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
            var val = ExpressionUtil.ConvertToCanonicalValue(context.Options, coreResult, out ValueKind kind, out Object raw, out ResultMemory conversionMemory);

            // The depth can be safely trimmed when the total size of the core result is known,
            // or when the total size of the core result can easily be determined.
            var trimDepth = coreMemory.IsTotal || (Object.ReferenceEquals(raw, null) && s_simpleKinds.Contains(kind));

            // Account for the memory overhead of the core result
            var coreBytes = coreMemory.Bytes ?? EvaluationMemory.CalculateBytes(raw ?? val);
            context.Memory.AddAmount(Level, coreBytes, trimDepth);

            // Account for the memory overhead of the conversion result
            if (!Object.ReferenceEquals(raw, null))
            {
                if (conversionMemory == null)
                {
                    conversionMemory = new ResultMemory();
                }

                var conversionBytes = conversionMemory.Bytes ?? EvaluationMemory.CalculateBytes(val);
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

        /// <summary>
        /// This function is intended only for ExpressionNode authors to call during evaluation.
        /// The EvaluationContext caches result-state specific to the evaluation instance.
        /// </summary>
        public Boolean EvaluateBoolean(EvaluationContext context)
        {
            return Evaluate(context).ConvertToBoolean(context);
        }

        /// <summary>
        /// This function is intended only for ExpressionNode authors to call during evaluation.
        /// The EvaluationContext caches result-state specific to the evaluation instance.
        /// </summary>
        public DateTimeOffset EvaluateDateTime(EvaluationContext context)
        {
            return Evaluate(context).ConvertToDateTime(context);
        }

        /// <summary>
        /// This function is intended only for ExpressionNode authors to call during evaluation.
        /// The EvaluationContext caches result-state specific to the evaluation instance.
        /// </summary>
        public Decimal EvaluateNumber(EvaluationContext context)
        {
            return Evaluate(context).ConvertToNumber(context);
        }

        /// <summary>
        /// This function is intended only for ExpressionNode authors to call during evaluation.
        /// The EvaluationContext caches result-state specific to the evaluation instance.
        /// </summary>
        public String EvaluateString(EvaluationContext context)
        {
            return Evaluate(context).ConvertToString(context);
        }

        /// <summary>
        /// This function is intended only for ExpressionNode authors to call during evaluation.
        /// The EvaluationContext caches result-state specific to the evaluation instance.
        /// </summary>
        public Version EvaluateVersion(EvaluationContext context)
        {
            return Evaluate(context).ConvertToVersion(context);
        }

        public virtual IEnumerable<T> GetParameters<T>() where T : IExpressionNode
        {
            return new T[0];
        }

        protected MemoryCounter CreateMemoryCounter(EvaluationContext context)
        {
            return new MemoryCounter(this, context.Options.MaxMemory);
        }

        private Object Evaluate(
            ITraceWriter trace,
            ISecretMasker secretMasker,
            Object state,
            EvaluationOptions options,
            out ValueKind kind,
            out Object raw)
        {
            if (Container != null)
            {
                // Do not localize. This is an SDK consumer error.
                throw new NotSupportedException($"Expected {nameof(IExpressionNode)}.{nameof(Evaluate)} to be called on root node only.");
            }

            ISecretMasker originalSecretMasker = secretMasker;
            try
            {
                // Evaluate
                secretMasker = secretMasker?.Clone() ?? new SecretMasker();
                trace = new EvaluationTraceWriter(trace, secretMasker);
                var context = new EvaluationContext(trace, secretMasker, state, options, this);
                trace.Info($"Evaluating: {ConvertToExpression()}");
                EvaluationResult result = Evaluate(context);

                // Trace the result
                TraceTreeResult(context, result.Value, result.Kind);

                kind = result.Kind;
                raw = result.Raw;
                return result.Value;
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

        private void TraceTreeResult(
            EvaluationContext context,
            Object result,
            ValueKind kind)
        {
            // Get the realized expression
            String realizedExpression = ConvertToRealizedExpression(context);

            // Format the result
            String traceValue = ExpressionUtil.FormatValue(context.SecretMasker, result, kind);

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
            ValueKind.DateTime,
            ValueKind.Null,
            ValueKind.Number,
            ValueKind.String,
            ValueKind.Version,
        };

        private String m_name;
    }
}
