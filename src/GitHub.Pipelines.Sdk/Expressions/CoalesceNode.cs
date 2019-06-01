using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal sealed class CoalesceNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            EvaluationResult result = null;
            foreach (ExpressionNode parameter in Parameters)
            {
                result = parameter.Evaluate(context);
                if (result.Kind == ValueKind.Null)
                {
                    continue;
                }

                if (result.Kind == ValueKind.String && String.IsNullOrEmpty(result.Value as String))
                {
                    continue;
                }

                break;
            }

            return result?.Value;
        }
    }
}
