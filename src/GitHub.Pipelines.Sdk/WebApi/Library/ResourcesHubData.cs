using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class ResourcesHubData
    {
        [DataMember]
        public IList<ResourceItem> ResourceItems { get; set; }

        [DataMember]
        public ResourceFilters ResourceFilters { get; set; }

        [DataMember]
        public ResourceFilterOptions ResourceFilterOptions { get; set; }

        [DataMember]
        public String ContinuationToken { get; set; }
    }
}
