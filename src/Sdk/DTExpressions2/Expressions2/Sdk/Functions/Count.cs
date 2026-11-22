using System;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class Count : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var items = Parameters[0].Evaluate(context);
            
            // Array
            if (items.TryGetCollectionInterface(out var collection) &&
                collection is IReadOnlyArray array)
            {
                return array.Count;
            }
            // Primitive
            else if (items.IsPrimitive)
            {
                return items.ConvertToString().Length;
            }
            // Otherwise return zero
            else
            {
                return 0;
            }
        }
    }
}
