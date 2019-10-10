using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions2.Sdk;
using GitHub.DistributedTask.Pipelines.Runtime;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    /// <summary>
    /// Named-value node used when evaluating graph-node conditions
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class GraphConditionNamedValue<TInstance> : NamedValue where TInstance : IGraphNodeInstance 
    {
        protected override Object EvaluateCore(
            EvaluationContext context,
            out ResultMemory resultMemory)
        {
            resultMemory = null;
            var graphContext = context.State as GraphExecutionContext<TInstance>;
            graphContext.Data.TryGetValue(Name, out var result);
            return result;
        }
    }
}
