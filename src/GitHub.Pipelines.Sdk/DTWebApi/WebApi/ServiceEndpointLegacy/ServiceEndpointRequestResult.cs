namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System;
    using System.Net;
    using System.Runtime.Serialization;

    using Newtonsoft.Json.Linq;

    [DataContract]
    public class ServiceEndpointRequestResult
    {
        [DataMember(EmitDefaultValue = false)]
        public JToken Result
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public HttpStatusCode StatusCode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ErrorMessage
        {
            get;
            set;
        }
    }
}
