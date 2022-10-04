using System;
using GitHub.DistributedTask.Pipelines.ContextData;

namespace GitHub.DistributedTask.Expressions2.Sdk.Functions.v1
{
    internal sealed class Split : Function
    {
        protected sealed override Boolean TraceFullyRealized => false;

        protected sealed override Object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            String val = Parameters[0].EvaluateString(context) as String ?? String.Empty;
            String split = Parameters[1].EvaluateString(context) as String ?? String.Empty;
            var ret = new ArrayContextData();
            foreach(var v in val.Split(split))
            {
                ret.Add(new StringContextData(v));
            }
            return ret;
        }
    }
}
