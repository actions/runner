using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Runtime;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class DependenciesContextNode<TInstance> : NamedValueNode where TInstance : IGraphNodeInstance 
    {
        protected override Object EvaluateCore(EvaluationContext context)
        {
            var graphContext = context.State as GraphExecutionContext<TInstance>;
            return graphContext.Dependencies;
        }
    }
}
