using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.LegacyFunctions
{
    internal sealed class LegacyAnd : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var result = default(EvaluationResult);
            foreach (var parameter in Parameters)
            {
                result = parameter.Evaluate(context);
                if (result.IsFalsy)
                {
                    return result.Value;
                }
            }

            return result?.Value;
        }
    }
}
