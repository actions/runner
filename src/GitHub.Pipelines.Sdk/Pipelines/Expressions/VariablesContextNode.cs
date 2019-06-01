using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Expressions;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions
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
