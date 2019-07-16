using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class ContainsValue : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var left = Parameters[0].Evaluate(context);

            if (left.TryGetCollectionInterface(out Object collection))
            {
                var right = Parameters[1].Evaluate(context);

                if (collection is IReadOnlyArray array)
                {
                    foreach (var item in array)
                    {
                        var itemResult = EvaluationResult.CreateIntermediateResult(context, item);

                        if (right.AbstractEqual(itemResult))
                        {
                            return true;
                        }
                    }
                }
                else if (collection is IReadOnlyObject obj)
                {
                    foreach (var value in obj.Values)
                    {
                        var valueResult = EvaluationResult.CreateIntermediateResult(context, value);

                        if (right.AbstractEqual(valueResult))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
