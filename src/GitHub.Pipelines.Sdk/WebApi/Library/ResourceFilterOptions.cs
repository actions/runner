using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class ResourceFilterOptions
    {
        public ResourceFilterOptions(IList<IdentityRef> identities = null)
        {
            this.ResourceTypes = new List<String> { ResourceTypeConstants.ServiceEndpoints };
            this.Identities = identities;
        }

        [DataMember]
        public IList<String> ResourceTypes { get; set; }

        [DataMember]
        public IList<IdentityRef> Identities { get; set; }
    }
}
