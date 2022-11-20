using System;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Expressions2.Sdk.Functions.v1;

namespace Runner.Server.Controllers
{
    public class CounterFunction : Function
    {
        protected override object EvaluateCore(EvaluationContext context, out ResultMemory memory)
        {
            memory = null;
            String left = Parameters[0].EvaluateString(context) as String ?? String.Empty;
            if(Parameters.Count == 2) {
                var seed = Parameters[1].Evaluate(context).ConvertToNumber();
                return seed;
            }
            return 0;
        }
    }
}