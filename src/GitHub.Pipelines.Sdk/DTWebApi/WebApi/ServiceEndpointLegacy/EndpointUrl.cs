using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Represents url of the service endpoint.
    /// </summary>
    [DataContract]
    public class EndpointUrl
    {
        /// <summary>
        /// Gets or sets the display name of service endpoint url.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the help text of service endpoint url.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string HelpText { get; set; }

        /// <summary>
        /// Gets or sets the value of service endpoint url.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri Value { get; set; }

        /// <summary>
        /// Gets or sets the visibility of service endpoint url.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string IsVisible { get; set; }

        /// <summary>
        /// Gets or sets the dependency bindings.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DependsOn DependsOn { get; set; }
    }
}