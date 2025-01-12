using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace Sdk.Pipelines
{
    public sealed class CancelledFunction : Function
    {
        protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
        {
            resultMemory = null;
            var templateContext = evaluationContext.State as TemplateContext;
            var executionContext = templateContext.State[nameof(ExecutionContext)] as ExecutionContext;
            return executionContext.Cancelled.IsCancellationRequested;
        }
    }
}
