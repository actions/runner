﻿using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public abstract class AzureResourcePermission : AzurePermission
    {
        [DataMember]
        public String ResourceGroup { get; set; }

        protected AzureResourcePermission(String resourceProvider) : base(resourceProvider)
        {
        }
    }
}
