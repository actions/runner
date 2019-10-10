using System;
using GitHub.DistributedTask.Expressions;
using GitHub.DistributedTask.Pipelines.Validation;

namespace GitHub.DistributedTask.Pipelines.Expressions
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
