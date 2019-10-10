using System;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace GitHub.DistributedTask.ObjectTemplating
{
    /// <summary>
    /// This expression node retrieves a user-defined named-value. This is used during expression evaluation.
    /// </summary>
    internal sealed class ContextValueNode : NamedValue
    {
        protected override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            return (context.State as TemplateContext).ExpressionValues[Name];
        }
    }
}
