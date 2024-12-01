using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Coalesce : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            string result = null;
            foreach (ExpressionNode parameter in Parameters)
            {
                result = parameter.EvaluateString(context);
                if (String.IsNullOrEmpty(result))
                {
                    continue;
                }
                break;
            }

            return result;
        }
    }
}
