namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class ServiceEndpointDetails
    {
        public ServiceEndpointDetails()
        {
        }

        private ServiceEndpointDetails(ServiceEndpointDetails endpointDetailsToClone)
        {
            this.Type = endpointDetailsToClone.Type;
            this.Url = endpointDetailsToClone.Url;

            if (endpointDetailsToClone.Authorization != null)
            {
                this.Authorization = endpointDetailsToClone.Authorization.Clone();
            }

            if (endpointDetailsToClone.data != null)
            {
                this.data = new Dictionary<String, String>(endpointDetailsToClone.data, StringComparer.OrdinalIgnoreCase);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Uri Url
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public EndpointAuthorization Authorization
        {
            get;
            set;
        }

        public IDictionary<String, String> Data
        {
            get
            {
                return this.data;
            }

            set
            {
                if (value != null)
                {
                    this.data = new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        public ServiceEndpointDetails Clone()
        {
            return new ServiceEndpointDetails(this);
        }

        [DataMember(EmitDefaultValue = false, Name = "Data")]
        private Dictionary<String, String> data;
    }
}
