using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class ContainsValue : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            EvaluationResult left = Parameters[0].Evaluate(context);

            if (left.TryGetCollectionInterface(out Object collection))
            {
                EvaluationResult right = Parameters[1].Evaluate(context);

                if (collection is IReadOnlyArray array)
                {
                    foreach (var item in array)
                    {
                        var itemResult = EvaluationResult.CreateIntermediateResult(context, item);

                        if (right.Equals(context, itemResult))
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

                        if (right.Equals(context, valueResult))
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
