using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class Resource
    {
        /// <summary>
        /// Initializes a new <c>Resource</c> instance with default properties.
        /// </summary>
        protected Resource()
        {
        }

        protected Resource(Resource resourceToCopy)
        {
            this.Alias = resourceToCopy.Alias;
            this.Endpoint = resourceToCopy.Endpoint?.Clone();
            m_properties = resourceToCopy.m_properties?.Clone();
        }

        /// <summary>
        /// Gets or sets the name of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Alias
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an optional endpoint used for connecting to the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ServiceEndpointReference Endpoint
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the extended properties set on the resource.
        /// </summary>
        public ResourceProperties Properties
        {
            get
            {
                if (m_properties == null)
                {
                    m_properties = new ResourceProperties();
                }
                return m_properties;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_properties?.Count == 0)
            {
                m_properties = null;
            }
        }

        [DataMember(Name = "Properties", EmitDefaultValue = false)]
        private ResourceProperties m_properties;
    }
}
