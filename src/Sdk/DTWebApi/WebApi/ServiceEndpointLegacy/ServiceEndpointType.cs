using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.FormInput;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Represents type of the service endpoint.
    /// </summary>
    [DataContract]
    public class ServiceEndpointType
    {
        /// <summary>
        /// Gets or sets the name of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }

        /// <summary>
        /// Gets or sets the display name of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description { get; set; }

        /// <summary>
        /// Gets or sets the endpoint url of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public EndpointUrl EndpointUrl { get; set; }

        public List<DataSource> DataSources => this.m_dataSources ?? (this.m_dataSources = new List<DataSource>());

        public List<DependencyData> DependencyData => this.m_dependencyData ?? (this.m_dependencyData = new List<DependencyData>());

        public List<String> TrustedHosts => this.m_trustedHosts ?? (this.m_trustedHosts = new List<String>());

        public List<ServiceEndpointAuthenticationScheme> AuthenticationSchemes => this.m_authenticationSchemes ?? (this.m_authenticationSchemes = new List<ServiceEndpointAuthenticationScheme>());

        /// <summary>
        /// Gets or sets the help link of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public HelpLink HelpLink { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String HelpMarkDown { get; set; }

        public List<InputDescriptor> InputDescriptors => this.m_inputDescriptors ?? (this.m_inputDescriptors = new List<InputDescriptor>());

        /// <summary>
        /// Gets or sets the icon url of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri IconUrl { get; set; }

        /// <summary>
        /// Gets or sets the ui contribution id of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string UiContributionId { get; set; }

        /// <summary>
        /// Input descriptor of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "InputDescriptors")]
        private List<InputDescriptor> m_inputDescriptors;

        /// <summary>
        /// Authentication scheme of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "AuthenticationSchemes")]
        private List<ServiceEndpointAuthenticationScheme> m_authenticationSchemes;

        /// <summary>
        /// Data sources of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "DataSources")]
        private List<DataSource> m_dataSources;

        /// <summary>
        /// Dependency data of service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "DependencyData")]
        private List<DependencyData> m_dependencyData;

        /// <summary>
        /// Trusted hosts of a service endpoint type.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Name = "TrustedHosts")]
        private List<String> m_trustedHosts;
    }
}
