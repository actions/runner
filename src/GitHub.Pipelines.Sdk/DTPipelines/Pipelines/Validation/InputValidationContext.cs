using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.Expressions;
using Microsoft.TeamFoundation.DistributedTask.Logging;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines.Validation
{
    /// <summary>
    /// Provides the necessary context for performing input value validation.
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class InputValidationContext
    {
        /// <summary>
        /// Gets or sets an expression which should be used to validate <see cref="Value"/>.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Expression
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not to evaluate the expression using <see cref="Value"/>.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean Evaluate
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the options used during expression evalation.
        /// </summary>
        public EvaluationOptions EvaluationOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the secret masker implementation.
        /// </summary>
        public ISecretMasker SecretMasker
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the trace writer implementation.
        /// </summary>
        public ITraceWriter TraceWriter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value which should be validated.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Value
        {
            get;
            set;
        }
    }
}
