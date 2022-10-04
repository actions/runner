using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Length : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            var left = Parameters[0].Evaluate(context);
            if (left.TryGetCollectionInterface(out Object collection))
            {
                if (collection is IReadOnlyArray array)
                {
                    return array.Count;
                }
                else if (collection is IReadOnlyObject obj)
                {
                    return obj.Count;
                }
            }
            return (left?.ConvertToString() ?? string.Empty).Length;
        }
    }
}
