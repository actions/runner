namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.Services.FormInput;


    [DataContract]
    public class ServiceEndpointAuthenticationScheme
    {
        /// <summary>
        /// Gets or sets the scheme for service endpoint authentication.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Scheme { get; set; }

        /// <summary>
        /// Gets or sets the display name for the service endpoint authentication scheme.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the authorization headers of service endpoint authentication scheme.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<AuthorizationHeader> AuthorizationHeaders
        {
            get { return m_authorizationHeaders ?? (m_authorizationHeaders = new List<AuthorizationHeader>()); }
            set { m_authorizationHeaders = value; }
        }

        /// <summary>
        /// Gets or sets the certificates of service endpoint authentication scheme.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<ClientCertificate> ClientCertificates
        {
            get { return m_clientCertificates ?? (m_clientCertificates = new List<ClientCertificate>()); }
            set { m_clientCertificates = value; }
        }

        public List<InputDescriptor> InputDescriptors
        {
            get { return m_inputDescriptors ?? (m_inputDescriptors = new List<InputDescriptor>()); }
            set { m_inputDescriptors = value; }
        }

        /// <summary>
        /// Gets or sets the input descriptors for the service endpoint authentication scheme.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "InputDescriptors")]
        private List<InputDescriptor> m_inputDescriptors;

        private List<AuthorizationHeader> m_authorizationHeaders;
        private List<ClientCertificate> m_clientCertificates;
    }
}