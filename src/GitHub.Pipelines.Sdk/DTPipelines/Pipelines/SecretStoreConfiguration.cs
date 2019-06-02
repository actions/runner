using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class SecretStoreConfiguration
    {
        public SecretStoreConfiguration()
        {
        }

        private SecretStoreConfiguration(SecretStoreConfiguration configurationToCopy)
        {
            this.Endpoint = configurationToCopy.Endpoint?.Clone();
            this.StoreName = configurationToCopy.StoreName;

            m_endpointId = configurationToCopy.m_endpointId;
            if (configurationToCopy.m_keys?.Count > 0)
            {
                m_keys = new List<String>(configurationToCopy.m_keys);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public ServiceEndpointReference Endpoint
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String StoreName
        {
            get;
            set;
        }

        public IList<String> Keys
        {
            get
            {
                if (m_keys == null)
                {
                    m_keys = new List<String>();
                }
                return m_keys;
            }
        }

        public SecretStoreConfiguration Clone()
        {
            return new SecretStoreConfiguration(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (this.Endpoint == null && m_endpointId != Guid.Empty)
            {
                this.Endpoint = new ServiceEndpointReference
                {
                    Id = m_endpointId,
                };
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_keys?.Count == 0)
            {
                m_keys = null;
            }

            if (this.Endpoint != null && this.Endpoint.Id != Guid.Empty)
            {
                m_endpointId = this.Endpoint.Id;
            }
        }

        [DataMember(Name = "EndpointId", EmitDefaultValue = false)]
        private Guid m_endpointId;

        [DataMember(Name = "Keys", EmitDefaultValue = false)]
        private List<String> m_keys;
    }
}
