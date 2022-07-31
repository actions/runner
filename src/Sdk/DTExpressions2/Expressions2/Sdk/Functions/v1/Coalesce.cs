using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Coalesce : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            EvaluationResult result = null;
            foreach (ExpressionNode parameter in Parameters)
            {
                result = parameter.Evaluate(context);
                if (result.Kind == ValueKind.Null)
                {
                    continue;
                }

                if (result.Kind == ValueKind.String && String.IsNullOrEmpty(result.Value as String))
                {
                    continue;
                }

                break;
            }

            return result?.Value;
        }
    }
}
