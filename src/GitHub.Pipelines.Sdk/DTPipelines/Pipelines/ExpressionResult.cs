using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    /// <summary>
    /// Represents the result of an <c>ExpressionValue&lt;T&gt;</c> evaluation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ExpressionResult<T>
    {
        /// <summary>
        /// Initializes a new <c>ExpressionResult</c> instance with the specified value. The value is implicilty treated as 
        /// non-secret.
        /// </summary>
        /// <param name="value">The resolved value</param>
        public ExpressionResult(T value)
            : this(value, false)
        { 
        }

        /// <summary>
        /// Initializes a new <c>ExpressionResult</c> instance with the specified values.
        /// </summary>
        /// <param name="value">The resolved value</param>
        /// <param name="containsSecrets">True if secrets were accessed while resolving the value; otherwise, false</param>
        public ExpressionResult(
            T value, 
            Boolean containsSecrets)
        {
            this.ContainsSecrets = containsSecrets;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not secrets were accessed while resolving <see cref="Value"/>.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean ContainsSecrets
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the literal value result.
        /// </summary>
        [DataMember]
        public T Value
        {
            get;
            set;
        }
    }
}
