using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.WebPlatform
{
    [DataContract]
    public class CustomerIntelligenceEvent
    {
        [DataMember]
        public String Area { get; set; }

        [DataMember]
        public String Feature { get; set; }

        [DataMember]
        public Dictionary<String, Object> Properties { get; set; }
    }
}
