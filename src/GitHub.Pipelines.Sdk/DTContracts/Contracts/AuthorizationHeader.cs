using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.Common.Contracts
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
