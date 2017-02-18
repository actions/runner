// This source file is maintained in two repos. Edits must be made to both copies.
// Unit tests live in the vsts-agent repo on GitHub.
//
// Repo 1) VSO repo under DistributedTask/Sdk/Server/Expressions
// Repo 2) vsts-agent repo on GitHub under src/Microsoft.VisualStudio.Services.Agent/DistributedTask.Expressions
//
// The style of this source file aims to follow VSO/DistributedTask conventions.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions
{
    public interface INode
    {
        Boolean EvaluateBoolean(ITraceWriter trace, object state);
    }

    public abstract class Node : INode
    {
        public abstract String Name { get; }

        internal ContainerNode Container { get; set; }

        internal Int32 Level { get; private set; }

        internal abstract String ConvertToExpression();

        internal abstract String ConvertToRealizedExpression(EvaluationContext context);

        protected abstract Object EvaluateCore(EvaluationContext context);

        // INode entry point.
        public Boolean EvaluateBoolean(ITraceWriter trace, Object state)
        {
            if (Container != null)
            {
                throw new NotSupportedException($"Expected {nameof(INode)}.{nameof(EvaluateBoolean)} to be called on root node only.");
            }

            var context = new EvaluationContext(trace, state);
            trace.Info($"Evaluating: {ConvertToExpression()}");
            Boolean result = EvaluateBoolean(context);
            trace.Info($"{ConvertToRealizedExpression(context)} => {result}");
            return result;
        }

        public EvaluationResult Evaluate(EvaluationContext context)
        {
            Level = Container == null ? 0 : Container.Level + 1;
            TraceVerbose(context, Level, $"Evaluating {Name}:");
            ValueKind kind;
            Object val = ConvertToCanonicalValue(EvaluateCore(context), out kind);
            var result = new EvaluationResult(context, Level, val, kind);
            context.Results[this] = result;
            return result;
        }

        public Boolean EvaluateBoolean(EvaluationContext context)
        {
            return Evaluate(context).ConvertToBoolean(context);
        }

        public Decimal EvaluateNumber(EvaluationContext context)
        {
            return Evaluate(context).ConvertToNumber(context);
        }

        public String EvaluateString(EvaluationContext context)
        {
            return Evaluate(context).ConvertToString(context);
        }

        public Version EvaluateVersion(EvaluationContext context)
        {
            return Evaluate(context).ConvertToVersion(context);
        }

        internal static Object ConvertToCanonicalValue(Object val, out ValueKind kind)
        {
            if (Object.ReferenceEquals(val, null))
            {
                kind = ValueKind.Null;
                return null;
            }
            else if (val is JToken)
            {
                var jtoken = val as JToken;
                switch (jtoken.Type)
                {
                    case JTokenType.Array:
                        kind = ValueKind.Array;
                        return jtoken;
                    case JTokenType.Boolean:
                        kind = ValueKind.Boolean;
                        return jtoken.ToObject<Boolean>();
                    case JTokenType.Float:
                    case JTokenType.Integer:
                        kind = ValueKind.Number;
                        // todo: test the extents of the conversion
                        return jtoken.ToObject<Decimal>();
                    case JTokenType.Null:
                        kind = ValueKind.Null;
                        return null;
                    case JTokenType.Object:
                        kind = ValueKind.Object;
                        return jtoken;
                    case JTokenType.String:
                        kind = ValueKind.String;
                        return jtoken.ToObject<String>();
                }
            }
            else if (val is String)
            {
                kind = ValueKind.String;
                return val;
            }
            else if (val is Version)
            {
                kind = ValueKind.Version;
                return val;
            }
            else if (!val.GetType().GetTypeInfo().IsClass)
            {
                if (val is Boolean)
                {
                    kind = ValueKind.Boolean;
                    return val;
                }
                else if (val is Decimal || val is Byte || val is SByte || val is Int16 || val is UInt16 || val is Int32 || val is UInt32 || val is Int64 || val is UInt64 || val is Single || val is Double)
                {
                    kind = ValueKind.Number;
                    // todo: test the extents of the conversion
                    return (Decimal)val;
                }
            }

            kind = ValueKind.Object;
            return val;
        }

        internal static void TraceVerbose(EvaluationContext context, Int32 level, String message)
        {
            context.Trace.Verbose(String.Empty.PadLeft(level * 2, '.') + (message ?? String.Empty));
        }
    }

    public enum ValueKind
    {
        Array,
        Boolean,
        Null,
        Number,
        Object,
        String,
        Version,
    }

    public sealed class EvaluationContext
    {
        internal EvaluationContext(ITraceWriter trace, Object state)
        {
            if (trace == null)
            {
                throw new ArgumentNullException(nameof(trace));
            }

            Trace = trace;
            State = state;
            Results = new Dictionary<Node, EvaluationResult>();
        }

        public ITraceWriter Trace { get; }

        public Object State { get; }

        internal Dictionary<Node, EvaluationResult> Results { get; }
    }

    public sealed class LiteralValueNode : Node
    {
        public LiteralValueNode(Object val)
        {
            ValueKind kind;
            Value = ConvertToCanonicalValue(val, out kind);
            Kind = kind;
        }

        public ValueKind Kind { get; }

        public sealed override String Name => Kind.ToString();

        public Object Value { get; }

        internal sealed override String ConvertToExpression()
        {
            switch (Kind)
            {
                case ValueKind.Boolean:
                    return ((Boolean)Value).ToString();

                case ValueKind.Number:
                    String d = ((Decimal)Value).ToString("G", CultureInfo.InvariantCulture);
                    if (d.Contains("."))
                    {
                        d = d.TrimEnd('0').TrimEnd('.'); // Omit trailing zeros after the decimal point.
                    }

                    return d;

                case ValueKind.String:
                    return String.Format(CultureInfo.InvariantCulture, "'{0}'", (Value as String).Replace("'", "''"));

                case ValueKind.Version:
                    return String.Format(CultureInfo.InvariantCulture, "v{0}", Value);

                case ValueKind.Array:
                case ValueKind.Null:
                case ValueKind.Object:
                    return Kind.ToString();

                default:
                    throw new NotSupportedException($"Unexpected kind '{Kind}' encountered when converting to expression.");
            }
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context) => ConvertToExpression();

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Value;
        }
    }

    public abstract class NamedValueNode : Node
    {
        internal sealed override string ConvertToExpression() => Name;

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            EvaluationResult evaluationResult;
            if (context.Results.TryGetValue(this, out evaluationResult))
            {
                return evaluationResult.ConvertToRealizedExpression();
            }

            return Name;
        }
    }

    public abstract class ContainerNode : Node
    {
        public IReadOnlyList<Node> Parameters => m_parameters.AsReadOnly();

        public void AddParameter(Node node)
        {
            m_parameters.Add(node);
            node.Container = this;
        }

        public void ReplaceParameter(Int32 index, Node node)
        {
            m_parameters[index] = node;
            node.Container = this;
        }

        private readonly List<Node> m_parameters = new List<Node>();
    }

    internal sealed class IndexerNode : ContainerNode
    {
        public sealed override String Name => "indexer";

        internal sealed override String ConvertToExpression()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}[{1}]",
                Parameters[0].ConvertToExpression(),
                Parameters[1].ConvertToExpression());
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            EvaluationResult evaluationResult;
            if (context.Results.TryGetValue(this, out evaluationResult))
            {
                return evaluationResult.ConvertToRealizedExpression();
            }

            return ConvertToExpression();
        }

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            Object result = null;
            EvaluationResult item = Parameters[0].Evaluate(context);
            if (item.Kind == ValueKind.Array && item.Value is JArray)
            {
                var jarray = item.Value as JArray;
                EvaluationResult index = Parameters[1].Evaluate(context);
                if (index.Kind == ValueKind.Number)
                {
                    Decimal d = (Decimal)index.Value;
                    if (d >= 0m && d < (Decimal)jarray.Count && d == Math.Floor(d))
                    {
                        result = jarray[(Int32)d];
                    }
                }
                else if (index.Kind == ValueKind.String && !String.IsNullOrEmpty(index.Value as String))
                {
                    Decimal d;
                    if (index.TryConvertToNumber(context, out d))
                    {
                        if (d >= 0m && d < (Decimal)jarray.Count && d == Math.Floor(d))
                        {
                            result = jarray[(Int32)d];
                        }
                    }
                }
            }
            else if (item.Kind == ValueKind.Object && item.Value is JObject)
            {
                var jobject = item.Value as JObject;
                EvaluationResult index = Parameters[1].Evaluate(context);
                String s;
                if (index.TryConvertToString(context, out s))
                {
                    result = jobject[s];
                }
            }
            else if (item.Kind == ValueKind.Object && item.Value is IReadOnlyDictionary<String, Object>)
            {
                var dictionary = item.Value as IReadOnlyDictionary<String, Object>;
                EvaluationResult index = Parameters[1].Evaluate(context);
                String s;
                if (index.TryConvertToString(context, out s))
                {
                    if (!dictionary.TryGetValue(s, out result))
                    {
                        result = null;
                    }
                }
            }

            return result;
        }
    }

    public abstract class FunctionNode : ContainerNode
    {
        internal virtual Boolean TraceFullyRealized => true;

        internal sealed override String ConvertToExpression()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}({1})",
                Name,
                String.Join(", ", Parameters.Select(x => x.ConvertToExpression())));
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            EvaluationResult evaluationResult;
            if (TraceFullyRealized && context.Results.TryGetValue(this, out evaluationResult))
            {
                return evaluationResult.ConvertToRealizedExpression();
            }

            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}({1})",
                Name,
                String.Join(", ", Parameters.Select(x => x.ConvertToRealizedExpression(context))));
        }
    }

    internal sealed class AndNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.And;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            foreach (Node parameter in Parameters)
            {
                if (!parameter.EvaluateBoolean(context))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal sealed class ContainsNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.Contains;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            String left = Parameters[0].EvaluateString(context) as String ?? String.Empty;
            String right = Parameters[1].EvaluateString(context) as String ?? String.Empty;
            return left.IndexOf(right, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    internal sealed class EndsWithNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.EndsWith;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            String left = Parameters[0].EvaluateString(context) ?? String.Empty;
            String right = Parameters[1].EvaluateString(context) ?? String.Empty;
            return left.EndsWith(right, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class EqualNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.Eq;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).Equals(context, Parameters[1].Evaluate(context));
        }
    }

    internal sealed class GreaterThanNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.GT;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) > 0;
        }
    }

    internal sealed class GreaterThanOrEqualNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.GE;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) >= 0;
        }
    }

    internal sealed class InNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.In;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            EvaluationResult left = Parameters[0].Evaluate(context);
            for (Int32 i = 1; i < Parameters.Count; i++)
            {
                EvaluationResult right = Parameters[i].Evaluate(context);
                if (left.Equals(context, right))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class LessThanNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.LT;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) < 0;
        }
    }

    internal sealed class LessThanOrEqualNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.LE;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) <= 0;
        }
    }

    internal sealed class NotEqualNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.NE;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return !Parameters[0].Evaluate(context).Equals(context, Parameters[1].Evaluate(context));
        }
    }

    internal sealed class NotNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.Not;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return !Parameters[0].EvaluateBoolean(context);
        }
    }

    internal sealed class NotInNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.NotIn;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            EvaluationResult left = Parameters[0].Evaluate(context);
            for (Int32 i = 1; i < Parameters.Count; i++)
            {
                EvaluationResult right = Parameters[i].Evaluate(context);
                if (left.Equals(context, right))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal sealed class OrNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.Or;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            foreach (Node parameter in Parameters)
            {
                if (parameter.EvaluateBoolean(context))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class StartsWithNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.StartsWith;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            String left = Parameters[0].EvaluateString(context) ?? String.Empty;
            String right = Parameters[1].EvaluateString(context) ?? String.Empty;
            return left.StartsWith(right, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class XorNode : FunctionNode
    {
        public sealed override String Name => ExpressionConstants.Xor;

        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].EvaluateBoolean(context) ^ Parameters[1].EvaluateBoolean(context);
        }
    }
}