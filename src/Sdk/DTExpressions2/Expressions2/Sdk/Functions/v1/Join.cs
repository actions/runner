using System;
using System.Text;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Join : Function
    {
        protected sealed override Boolean TraceFullyRealized => true;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory rmemory)
        {
            rmemory = null;
            var items = Parameters[1].Evaluate(context);

            if (items.TryGetCollectionInterface(out var collection) && collection is IReadOnlyArray array)
            {
                if (array.Count > 0)
                {
                    var result = new StringBuilder();
                    var memory = new MemoryCounter(this, context.Options.MaxMemory);

                    // Append the first item
                    var item = array[0];
                    var itemResult = EvaluationResult.CreateIntermediateResult(context, item);
                    if (itemResult.TryConvertToString(context, out String itemString))
                    {
                        memory.Add(itemString);
                        result.Append(itemString);
                    }

                    // More items?
                    if (array.Count > 1)
                    {
                        var separator = Parameters[0].EvaluateString(context);

                        for (var i = 1; i < array.Count; i++)
                        {
                            // Append the separator
                            memory.Add(separator);
                            result.Append(separator);

                            // Append the next item
                            var nextItem = array[i];
                            var nextItemResult = EvaluationResult.CreateIntermediateResult(context, nextItem);
                            if (nextItemResult.TryConvertToString(context, out String nextItemString))
                            {
                                memory.Add(nextItemString);
                                result.Append(nextItemString);
                            }
                        }
                    }

                    return result.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
            else if (items.TryConvertToString(context, out String str))
            {
                return str;
            }
            else
            {
                return String.Empty;
            }
        }
    }
}
