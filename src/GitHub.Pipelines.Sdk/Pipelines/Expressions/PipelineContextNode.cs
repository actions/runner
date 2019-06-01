using System;
using System.ComponentModel;
using System.Collections.Generic;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal sealed class PipelineContextNode : NamedValueNode 
    {
        protected override Object EvaluateCore(EvaluationContext context)
        {
            var state = context.State as IPipelineContext;
            var result = new Dictionary<String, Object>(StringComparer.OrdinalIgnoreCase);

            // startTime
            if (state.Variables.TryGetValue(WellKnownDistributedTaskVariables.PipelineStartTime, out VariableValue startTimeVariable) &&
                !String.IsNullOrEmpty(startTimeVariable.Value))
            {
                // Leverage the expression SDK to convert to datetime
                var startTimeResult = EvaluationResult.CreateIntermediateResult(context, startTimeVariable.Value, out _);
                if (startTimeResult.TryConvertToDateTime(context, out DateTimeOffset startTime))
                {
                    result["startTime"] = startTime;
                }
            }

            return result;
        }
    }
}
