using System;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.TeamFoundation.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// This expression node retrieves a user-defined named-value. This is used during expression evaluation.
    /// </summary>
    internal sealed class ContextValueNode : NamedValueNode
    {
        protected override Object EvaluateCore(EvaluationContext context)
        {
            return (context.State as TemplateContext).ExpressionValues[Name];
        }
    }
}
