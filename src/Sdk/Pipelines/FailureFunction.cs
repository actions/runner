using System.Linq;
using GitHub.DistributedTask.WebApi;
using GitHub.DistributedTask.ObjectTemplating;
using GitHub.DistributedTask.Expressions2.Sdk;

namespace Sdk.Pipelines
{
    public sealed class FailureFunction : Function
    {
        protected sealed override object EvaluateCore(EvaluationContext evaluationContext, out ResultMemory resultMemory)
        {
            resultMemory = null;
            var templateContext = evaluationContext.State as TemplateContext;
            var executionContext = templateContext.State[nameof(ExecutionContext)] as ExecutionContext;
            if(executionContext.Cancelled.IsCancellationRequested) {
                return false;
            }
            if(Parameters?.Any() ?? false) {
                foreach(var parameter in Parameters) {
                    var s = parameter.Evaluate(evaluationContext).ConvertToString();
                    JobItemFacade item = null;
                    if(executionContext.JobContext.TryGetDependency(s, out item)) {
                        if(item?.Status == TaskResult.Failed) {
                            return true;
                        }
                    }
                }
                return false;
            }
            return executionContext.JobContext.Failure;
        }
    }
}
