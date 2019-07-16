using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace GitHub.DistributedTask.Expressions2.Sdk
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class Function : Container
    {
        /// <summary>
        /// Generally this should not be overridden. True indicates the result of the node is traced as part of the "expanded"
        /// (i.e. "realized") trace information. Otherwise the node expression is printed, and parameters to the node may or
        /// may not be fully realized - depending on each respective parameter's trace-fully-realized setting.
        /// 
        /// The purpose is so the end user can understand how their expression expanded at run time. For example, consider
        /// the expression: eq(variables.publish, 'true'). The runtime-expanded expression may be: eq('true', 'true')
        /// </summary>
        protected override Boolean TraceFullyRealized => true;

        internal sealed override String ConvertToExpression()
        {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}({1})",
                Name,
                String.Join(", ", Parameters.Select(x => x.ConvertToExpression())));
        }

        internal sealed override String ConvertToRealizedExpression(EvaluationContext context)
        {
            // Check if the result was stored
            if (context.TryGetTraceResult(this, out String result))
            {
                return result;
            }

            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}({1})",
                Name,
                String.Join(", ", Parameters.Select(x => x.ConvertToRealizedExpression(context))));
        }
    }
}
