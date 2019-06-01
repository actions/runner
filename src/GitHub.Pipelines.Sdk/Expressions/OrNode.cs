using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal sealed class OrNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            foreach (ExpressionNode parameter in Parameters)
            {
                if (parameter.EvaluateBoolean(context))
                {
                    return true;
                }
            }

            return false;
        }
    }
}