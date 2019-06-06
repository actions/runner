namespace GitHub.DistributedTask.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class AuthenticationSchemeReference
    {
        [DataMember]
        public String Type { get; set; }

        [DataMember]
        public Dictionary<String, String> Inputs { get; set; }
    }
}
