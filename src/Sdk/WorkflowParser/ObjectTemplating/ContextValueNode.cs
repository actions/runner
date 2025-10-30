#nullable disable // Consider removing in the future to minimize likelihood of NullReferenceException; refer https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references

using System;
using GitHub.Actions.Expressions.Sdk;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating
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
