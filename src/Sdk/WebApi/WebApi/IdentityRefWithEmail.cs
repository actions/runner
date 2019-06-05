using System;
using System.Runtime.Serialization;

namespace GitHub.Services.WebApi
{
    [DataContract]
    public class IdentityRefWithEmail : IdentityRef
    {
        [DataMember(Name = "preferredEmailAddress", EmitDefaultValue = false)]
        public String PreferredEmailAddress { get; set; }
    }
}
