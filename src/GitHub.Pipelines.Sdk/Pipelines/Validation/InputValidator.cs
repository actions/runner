using System;
using System.ComponentModel;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.Expressions;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation
{
    /// <summary>
    /// Provides n validator implementation for task inputs.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class InputValidator : IInputValidator
    {
        public InputValidationResult Validate(InputValidationContext context)
        {
            if (String.IsNullOrEmpty(context.Expression))
            {
                return InputValidationResult.Succeeded;
            }

            var result = new InputValidationResult();
            try
            {
                var parser = new ExpressionParser();
                var tree = parser.CreateTree(context.Expression, context.TraceWriter, namedValues: InputValidationConstants.NamedValues, functions: InputValidationConstants.Functions);
                if (context.Evaluate)
                {
                    result.IsValid = tree.Evaluate<Boolean>(context.TraceWriter, context.SecretMasker, context, context.EvaluationOptions);
                }
                else
                {
                    result.IsValid = true;
                }
            }
            catch (Exception ex) when (ex is ParseException || ex is RegularExpressionInvalidOptionsException || ex is NotSupportedException)
            {
                result.Reason = ex.Message;
            }

            return result;
        }
    }
}
