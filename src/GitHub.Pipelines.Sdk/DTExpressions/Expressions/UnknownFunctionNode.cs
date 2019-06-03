using System;

namespace GitHub.DistributedTask.Expressions
{
    internal sealed class UnknownFunctionNode : FunctionNode
    {
        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            // Should never reach here.
            throw new NotSupportedException("Unknown function node is not supported during evaluation.");
        }
    }
}
