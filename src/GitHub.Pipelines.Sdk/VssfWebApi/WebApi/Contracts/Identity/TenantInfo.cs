using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace GitHub.Services.Identity
{
    [DebuggerDisplay("{TenantName}")]
    [DataContract]
    public class TenantInfo
    {
        [DataMember]
        public Guid TenantId { get; set; }

        [DataMember]
        public string TenantName { get; set; }

        [DataMember]
        public bool HomeTenant { get; set; }

        [DataMember]
        public IEnumerable<string> VerifiedDomains { get; set; }
    }
}
