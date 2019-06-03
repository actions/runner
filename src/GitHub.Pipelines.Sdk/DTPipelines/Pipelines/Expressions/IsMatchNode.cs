using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions;

namespace GitHub.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class IsMatchNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        public static Int32 minParameters = 2;
        public static Int32 maxParameters = 3;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            // isMatch(value: string, regEx: string, options?: string) 
            String value = Parameters[0].EvaluateString(context) ?? String.Empty;
            String regEx = Parameters[1].EvaluateString(context) ?? String.Empty;
            String regExOptionsString = String.Empty;

            if (Parameters.Count == 3)
            {
                regExOptionsString = Parameters[2].EvaluateString(context) ?? String.Empty;
            }

            return RegexUtility.IsMatch(value, regEx, regExOptionsString);
        }
    }
}
