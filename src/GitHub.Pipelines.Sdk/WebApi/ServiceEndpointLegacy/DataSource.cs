namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class DataSource
    {
        [DataMember]
        public String Name { get; set; }

        [DataMember]
        public String EndpointUrl { get; set; }

        [DataMember]
        public String ResourceUrl { get; set; }

        [DataMember]
        public String ResultSelector { get; set; }

        [DataMember]
        public List<AuthorizationHeader> Headers { get; set; }

        [DataMember]
        public AuthenticationSchemeReference AuthenticationScheme { get; set; }
    }
}