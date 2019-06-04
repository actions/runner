using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.Pipelines.Runtime;

namespace GitHub.DistributedTask.Pipelines.Expressions
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
