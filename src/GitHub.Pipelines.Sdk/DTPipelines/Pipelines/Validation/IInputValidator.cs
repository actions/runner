using System.ComponentModel;

namespace GitHub.DistributedTask.Pipelines.Validation
{
    /// <summary>
    /// Provides a contract validators must implement to participate in input validation.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IInputValidator
    {
        /// <summary>
        /// Validates the input value using the provided context.
        /// </summary>
        /// <param name="context">The current input validation context</param>
        /// <returns>A result which indicates success or failure of the validation in addition to detailed reason on failure</returns>
        InputValidationResult Validate(InputValidationContext context);
    }
}
