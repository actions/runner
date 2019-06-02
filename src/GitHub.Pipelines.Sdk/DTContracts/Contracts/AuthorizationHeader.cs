using System;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.Common.Contracts
{
    [DataContract]
    public class AuthorizationHeader : BaseSecuredObject
    {
        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public String Value { get; set; }
    }
}