using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions
{
    internal sealed class Split : Function
    {
        protected sealed override Boolean TraceFullyRealized => true;

        protected sealed override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var subject = Parameters[0].Evaluate(context).ConvertToString();
            var separator = ",";
            if (Parameters.Count > 1)
            {
                var separatorResult = Parameters[1].Evaluate(context);
                if (separatorResult.IsPrimitive)
                {
                    separator = separatorResult.ConvertToString();
                }
            }
            
            var result = new FilteredArray();
            var memory = new MemoryCounter(this, context.Options.MaxMemory);
            
            var chunks = subject.Split(separator);
            foreach (var chunk in chunks)
            {
                var partialResult = EvaluationResult.CreateIntermediateResult(context, chunk);
                var partialString = partialResult.ConvertToString();
                memory.Add(partialString);
                result.Add(partialString);
            }

            resultMemory = new ResultMemory { Bytes = memory.CurrentBytes };
            return result;
        }
    }
}
