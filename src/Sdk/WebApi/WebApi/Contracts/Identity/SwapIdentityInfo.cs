using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Identity
{
    [DataContract]
    public class SwapIdentityInfo
    {
        public SwapIdentityInfo()
        {
        }

        public SwapIdentityInfo(Guid id1, Guid id2)
        {
            this.Id1 = id1;
            this.Id2 = id2;
        }

        [DataMember]
        public Guid Id1 { get; private set; }

        [DataMember]
        public Guid Id2 { get; private set; }
    }
}
