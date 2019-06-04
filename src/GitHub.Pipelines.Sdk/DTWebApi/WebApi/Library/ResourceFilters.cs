using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ResourceFilters
    {
        [DataMember]
        public IList<String> ResourceType { get; set; }

        [DataMember]
        public IList<Guid> CreatedBy { get; set; }

        [DataMember]
        public String SearchText { get; set; }
    }
}
