using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    internal sealed class UnknownNamedValueNode : NamedValueNode
    {
        protected sealed override object EvaluateCore(EvaluationContext evaluationContext)
        {
            // Should never reach here.
            throw new NotSupportedException("Unknown function node is not supported during evaluation.");
        }
    }
}
