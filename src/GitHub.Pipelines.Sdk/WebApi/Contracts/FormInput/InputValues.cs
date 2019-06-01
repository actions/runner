using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.FormInput
{
    /// <summary>
    /// Information about the possible/allowed values for a given subscription input
    /// </summary>
    [DataContract]
    public class InputValues : ISecuredObject
    {
        /// <summary>
        /// The id of the input
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String InputId { get; set; }

        /// <summary>
        /// The default value to use for this input
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DefaultValue { get; set; }

        /// <summary>
        /// Possible values that this input can take
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IList<InputValue> PossibleValues { get; set; }

        /// <summary>
        /// Should the value be restricted to one of the values in the PossibleValues (True)
        /// or are the values in PossibleValues just a suggestion (False)
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IsLimitedToPossibleValues { get; set; }

        /// <summary>
        /// Should this input be disabled
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IsDisabled { get; set; }

        /// <summary>
        /// Should this input be made read-only
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IsReadOnly { get; set; }

        /// <summary>
        /// Errors encountered while computing dynamic values.
        /// </summary>
        /// <returns></returns>
        [DataMember(EmitDefaultValue = false)]
        public InputValuesError Error { get; set; }

        public void SetSecuredObjectProperties(Guid namespaceId, Int32 requiredPermissions, String token)
        {
            this.m_namespaceId = namespaceId;
            this.m_requiredPermissions = requiredPermissions;
            this.m_token = token;

            this.Error?.SetSecuredObjectProperties(namespaceId, requiredPermissions, token);
            if (this.PossibleValues != null && this.PossibleValues.Any())
            {
                foreach (var value in this.PossibleValues)
                {
                    value.SetSecuredObjectProperties(namespaceId, requiredPermissions, token);
                }
            }
        }

        public Guid NamespaceId => m_namespaceId;

        public Int32 RequiredPermissions => m_requiredPermissions;

        public String GetToken()
        {
            return m_token;
        }

        private Guid m_namespaceId;
        private Int32 m_requiredPermissions;
        private String m_token;
    }

    /// <summary>
    /// Information about a single value for an input
    /// </summary>
    [DataContract]
    public class InputValue : ISecuredObject
    {
        /// <summary>
        /// The value to store for this input
        /// </summary>
        [DataMember]
        public String Value { get; set; }

        /// <summary>
        /// The text to show for the display of this value
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DisplayValue { get; set; }

        /// <summary>
        /// Any other data about this input
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IDictionary<String, object> Data { get; set; }

        public void SetSecuredObjectProperties(Guid namespaceId, Int32 requiredPermissions, String token)
        {
            this.m_namespaceId = namespaceId;
            this.m_requiredPermissions = requiredPermissions;
            this.m_token = token;
        }

        public Guid NamespaceId => m_namespaceId;

        public Int32 RequiredPermissions => m_requiredPermissions;

        public String GetToken()
        {
            return m_token;
        }

        private Guid m_namespaceId;
        private Int32 m_requiredPermissions;
        private String m_token;
    }

    /// <summary>
    /// Error information related to a subscription input value.
    /// </summary>
    [DataContract]
    public class InputValuesError : ISecuredObject
    {
        /// <summary>
        /// The error message.
        /// </summary>
        [DataMember]
        public String Message { get; set; }

        public void SetSecuredObjectProperties(Guid namespaceId, Int32 requiredPermissions, String token)
        {
            this.m_namespaceId = namespaceId;
            this.m_requiredPermissions = requiredPermissions;
            this.m_token = token;
        }

        public Guid NamespaceId => m_namespaceId;

        public Int32 RequiredPermissions => m_requiredPermissions;

        public String GetToken()
        {
            return m_token;
        }

        private Guid m_namespaceId;
        private Int32 m_requiredPermissions;
        private String m_token;
    }
}