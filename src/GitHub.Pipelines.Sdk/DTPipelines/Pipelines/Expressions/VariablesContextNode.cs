using System;
using System.ComponentModel;
using GitHub.DistributedTask.Expressions;

namespace GitHub.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class VariablesContextNode : NamedValueNode
    {
        protected override Object EvaluateCore(EvaluationContext context)
        {
            var executionContext = context.State as IPipelineContext;
            return executionContext.Variables;
        }
    }
}
