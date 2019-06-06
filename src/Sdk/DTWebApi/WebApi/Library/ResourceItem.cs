using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Resources include Service Connections, Variable Groups and Secure Files.
    /// </summary>
    [DataContract]
    public class ResourceItem
    {
        /// <summary>
        /// Gets or sets Id of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Id
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets name of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets resource type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String ResourceType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets icon url of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri IconUrl
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets description of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the identity who created the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef CreatedBy
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether resource is shared with other projects or not.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean IsShared
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets internal properties of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IDictionary<String, String> Properties
        {
            get;
            set;
        }
    }
}
