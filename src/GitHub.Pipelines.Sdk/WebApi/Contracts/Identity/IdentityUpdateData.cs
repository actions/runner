using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Identity
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