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
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Expressions
{
    public interface INode
    {
        Boolean EvaluateBoolean(ITraceWriter trace, object state);
    }

    public abstract class Node : INode
    {
        internal ContainerNode Container { get; set; }

        internal Int32 Level { get; private set; }

        internal virtual String Name { get; set; }

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
            var result = new EvaluationResult(context, Level, EvaluateCore(context));
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

        internal static void TraceVerbose(EvaluationContext context, Int32 level, String message)
        {
            context.Trace.Verbose(String.Empty.PadLeft(level * 2, '.') + (message ?? String.Empty));
        }
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

    public sealed class LeafNode : Node
    {
        public LeafNode(Object val)
        {
            // ConvertToExpression() depends on this constraint.
            if (!(val is Boolean) && !(val is Decimal) && !(val is String) && !(val is Version))
            {
                throw new NotSupportedException($"Unexpected leaf node object type: '{val?.GetType().FullName}'");
            }

            Value = val;
        }

        public Object Value { get; }

        internal sealed override String Name => "leaf";

        internal sealed override String ConvertToExpression()
        {
            if (Value is Boolean)
            {
                return ((Boolean)Value).ToString();
            }
            else if (Value is Decimal)
            {
                String result = ((Decimal)Value).ToString("G", CultureInfo.InvariantCulture);
                if (result.Contains("."))
                {
                    result = result.TrimEnd('0').TrimEnd('.'); // Omit trailing zeros after the decimal point.
                }

                return result;
            }
            else if (Value is String)
            {
                return String.Format(CultureInfo.InvariantCulture, "'{0}'", (Value as String).Replace("'", "''"));
            }
            else
            {
                return String.Format(CultureInfo.InvariantCulture, "v{0}", Value);
            }
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context) => ConvertToExpression();

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Value;
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
        internal sealed override String Name => "indexer";

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
        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).Equals(context, Parameters[1].Evaluate(context));
        }
    }

    internal sealed class GreaterThanNode : FunctionNode
    {
        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) > 0;
        }
    }

    internal sealed class GreaterThanOrEqualNode : FunctionNode
    {
        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) >= 0;
        }
    }

    internal sealed class InNode : FunctionNode
    {
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
        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) < 0;
        }
    }

    internal sealed class LessThanOrEqualNode : FunctionNode
    {
        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].Evaluate(context).CompareTo(context, Parameters[1].Evaluate(context)) <= 0;
        }
    }

    internal sealed class NotEqualNode : FunctionNode
    {
        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return !Parameters[0].Evaluate(context).Equals(context, Parameters[1].Evaluate(context));
        }
    }

    internal sealed class NotNode : FunctionNode
    {
        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return !Parameters[0].EvaluateBoolean(context);
        }
    }

    internal sealed class NotInNode : FunctionNode
    {
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
        internal sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].EvaluateBoolean(context) ^ Parameters[1].EvaluateBoolean(context);
        }
    }
}