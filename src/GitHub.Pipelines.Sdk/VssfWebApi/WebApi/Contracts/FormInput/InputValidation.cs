using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.FormInput
{
    /// <summary>
    /// Describes what values are valid for a subscription input
    /// </summary>
    [DataContract]
    public class InputValidation : ISecuredObject
    {
        /// <summary>
        /// Gets or sets the data data type to validate.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public InputDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets if this is a required field.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IsRequired { get; set; }

        /// <summary>
        /// Gets or sets the pattern to validate.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Pattern { get; set; }

        /// <summary>
        /// Gets or sets the error on pattern mismatch.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String PatternMismatchErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for this descriptor.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Decimal? MinValue { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for this descriptor.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Decimal? MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of this descriptor.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? MinLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of this descriptor.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? MaxLength { get; set; }

        public void SetSecuredObjectProperties(Guid namespaceId, int requiredPermissions, string token)
        {
            this.m_namespaceId = namespaceId;
            this.m_requiredPermissions = requiredPermissions;
            this.m_token = token;
        }

        public Guid NamespaceId => m_namespaceId;

        public int RequiredPermissions => m_requiredPermissions;

        public string GetToken()
        {
            return m_token;
        }

        private Guid m_namespaceId;
        private int m_requiredPermissions;
        private string m_token;
    }
}
