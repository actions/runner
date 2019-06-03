using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines.Validation
{
    /// <summary>
    /// Provides information about the result of input validation.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class InputValidationResult
    {
        public InputValidationResult()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the input value is valid.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IsValid
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating a detailed reason the input value is not valid.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Reason
        {
            get;
            set;
        }

        /// <summary>
        /// Provides a convenience property to return successful validation results.
        /// </summary>
        public static readonly InputValidationResult Succeeded = new InputValidationResult { IsValid = true };
    }
}
