using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class LengthNode : FunctionNode
    {
        protected sealed override Boolean TraceFullyRealized => false;

        public static Int32 minParameters = 1;
        public static Int32 maxParameters = 1;

        protected sealed override Object EvaluateCore(EvaluationContext context)
        {
            // Length(value: object) 
            var evaluationResult = Parameters[0].Evaluate(context);
            bool kindNotSupported = false;
            Int32 length = -1;

            switch (evaluationResult.Kind)
            {
                case ValueKind.Array:
                    length = ((JArray)evaluationResult.Value).Count;
                    break;
                case ValueKind.String:
                    length = ((String)evaluationResult.Value).Length;
                    break;
                case ValueKind.Object:
                    if (evaluationResult.Value is IReadOnlyDictionary<String, Object>)
                    {
                        length = ((IReadOnlyDictionary<String, Object>)evaluationResult.Value).Count;
                    }
                    else if (evaluationResult.Value is ICollection)
                    {
                        length = ((ICollection)evaluationResult.Value).Count;
                    }
                    else
                    {
                        kindNotSupported = true;
                    }
                    break;
                case ValueKind.Boolean:
                case ValueKind.Null:
                case ValueKind.Number:
                case ValueKind.Version:
                    kindNotSupported = true;
                    break;
            }

            if (kindNotSupported)
            {
                throw new NotSupportedException(PipelineStrings.InvalidTypeForLengthFunction(evaluationResult.Kind));
            }

            return new Decimal(length);
        }
    }
}
