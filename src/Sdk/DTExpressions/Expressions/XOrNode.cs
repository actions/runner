using System;

namespace GitHub.DistributedTask.Expressions
{
    internal sealed class XorNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return Parameters[0].EvaluateBoolean(context) ^ Parameters[1].EvaluateBoolean(context);
        }
    }
}
