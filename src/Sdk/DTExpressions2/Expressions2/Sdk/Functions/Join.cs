using System;
using System.Text;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class Join : Function
    {
        protected sealed override Boolean TraceFullyRealized => true;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var items = Parameters[0].Evaluate(context);

            // Array
            if (items.TryGetCollectionInterface(out var collection) &&
                collection is IReadOnlyArray array &&
                array.Count > 0)
            {
                var result = new StringBuilder();
                var memory = new MemoryCounter(this, context.Options.MaxMemory);

                // Append the first item
                var item = array[0];
                var itemResult = EvaluationResult.CreateIntermediateResult(context, item);
                var itemString = itemResult.ConvertToString();
                memory.Add(itemString);
                result.Append(itemString);

                // More items?
                if (array.Count > 1)
                {
                    var separator = ",";
                    if (Parameters.Count > 1)
                    {
                        var separatorResult = Parameters[1].Evaluate(context);
                        if (separatorResult.IsPrimitive)
                        {
                            separator = separatorResult.ConvertToString();
                        }
                    }

                    for (var i = 1; i < array.Count; i++)
                    {
                        // Append the separator
                        memory.Add(separator);
                        result.Append(separator);

                        // Append the next item
                        var nextItem = array[i];
                        var nextItemResult = EvaluationResult.CreateIntermediateResult(context, nextItem);
                        var nextItemString = nextItemResult.ConvertToString();
                        memory.Add(nextItemString);
                        result.Append(nextItemString);
                    }
                }

                return result.ToString();
            }
            // Primitive
            else if (items.IsPrimitive)
            {
                return items.ConvertToString();
            }
            // Otherwise return empty string
            else
            {
                return String.Empty;
            }
        }
    }
}
