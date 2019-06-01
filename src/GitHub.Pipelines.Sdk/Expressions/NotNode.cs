using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal sealed class NotNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            return !Parameters[0].EvaluateBoolean(context);
        }
    }
}
