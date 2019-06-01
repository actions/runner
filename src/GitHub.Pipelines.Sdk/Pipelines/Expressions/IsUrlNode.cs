using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class IsUrlNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        public static Int32 minParameters = 1;
        public static Int32 maxParameters = 1;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            // isUrl(value: string) 
            String value = Parameters[0].EvaluateString(context) ?? String.Empty;
            return RegexUtility.IsMatch(value, WellKnownRegularExpressions.Url);
        }
    }
}
