using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.AadMemberAccessStatus
{
    [DataContract]
    public sealed class AadMemberStatus
    {
        [DataMember]
        public AadMemberAccessState MemberState { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public DateTimeOffset StatusValidUntil { get; set; }
    }
}
