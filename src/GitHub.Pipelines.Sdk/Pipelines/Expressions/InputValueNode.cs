using System;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions
{
    internal class InputValueNode : NamedValueNode
    {
        protected sealed override Object EvaluateCore(EvaluationContext evaluationContext)
        {
            var validationContext = evaluationContext.State as InputValidationContext;
            return validationContext.Value;
        }
    }
}
