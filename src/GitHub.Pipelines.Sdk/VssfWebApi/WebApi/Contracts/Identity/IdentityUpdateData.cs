using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Identity
{
    [DataContract]
    public class IdentityUpdateData
    {
        [DataMember]
        public Int32 Index { get; set; }

        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public Boolean Updated { get; set; }
    }
}
