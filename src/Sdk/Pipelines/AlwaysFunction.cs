using System;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace Sdk.Pipelines
{
    public sealed class AlwaysFunction : Function
    {
        protected override Object EvaluateCore(EvaluationContext context, out ResultMemory resultMemory)
        {
            resultMemory = null;
            return true;
        }
    }
}
