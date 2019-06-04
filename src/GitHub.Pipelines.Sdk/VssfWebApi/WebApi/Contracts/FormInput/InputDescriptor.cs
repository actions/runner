using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.FormInput
{
    /// <summary>
    /// Describes an input for subscriptions.
    /// </summary>
    [DataContract]
    public class InputDescriptor : ISecuredObject
    {
        /// <summary>
        /// Identifier for the subscription input
        /// </summary>
        [DataMember]
        public String Id { get; set; }

        /// <summary>
        /// Localized name which can be shown as a label for the subscription input
        /// </summary>
        [DataMember]
        public String Name { get; set; }

        /// <summary>
        /// Description of what this input is used for
        /// </summary>
        [DataMember]
        public String Description { get; set; }

        /// <summary>
        /// Underlying data type for the input value. When this value is specified,
        /// InputMode, Validation and Values are optional.
        /// </summary>
        [DataMember]
        public string Type { get; set; }

        /// <summary>
        /// List of scopes supported.  Null indicates all scopes are supported.
        /// </summary>
        public List<String> SupportedScopes { get; set; }

        /// <summary>
        /// Custom properties for the input which can be used by the service provider
        /// </summary>
        [DataMember]
        public IDictionary<string, object> Properties { get; set; }

        /// <summary>
        /// Mode in which the value of this input should be entered
        /// </summary>
        [DataMember]
        public InputMode InputMode { get; set; }

        /// <summary>
        /// Gets whether this input is confidential, such as for a password or application key
        /// </summary>
        [DataMember]
        public Boolean IsConfidential { get; set; }

        /// <summary>
        /// Gets whether this input is included in the default generated action description.
        /// </summary>
        /// <returns></returns>
        [DataMember]
        public Boolean UseInDefaultDescription { get; set; }

        /// <summary>
        /// The group localized name to which this input belongs and can be shown as a header
        /// for the container that will include all the inputs in the group.
        /// </summary>
        [DataMember]
        public String GroupName { get; set; }

        /// <summary>
        /// A hint for input value. It can be used in the UI as the input placeholder.
        /// </summary>
        [DataMember]
        public String ValueHint { get; set; }

        /// <summary>
        /// Information to use to validate this input's value
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public InputValidation Validation { get; set; }

        /// <summary>
        /// Information about possible values for this input
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public InputValues Values { get; set; }

        /// <summary>
        /// The ids of all inputs that the value of this input is dependent on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IList<String> DependencyInputIds { get; set; }

        /// <summary>
        /// If true, the value information for this input is dynamic and
        /// should be fetched when the value of dependency inputs change.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean HasDynamicValueInformation { get; set; }

        public void SetSecuredObjectProperties(Guid namespaceId, Int32 requiredPermissions, String token)
        {
            this.m_namespaceId = namespaceId;
            this.m_requiredPermissions = requiredPermissions;
            this.m_token = token;

            this.Validation?.SetSecuredObjectProperties(namespaceId, requiredPermissions, token);
            this.Values?.SetSecuredObjectProperties(namespaceId, requiredPermissions, token);
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
