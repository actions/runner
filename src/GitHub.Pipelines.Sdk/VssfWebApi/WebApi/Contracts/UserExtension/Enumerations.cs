using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.UserAccountMapping
{
    [Flags, DataContract]
    public enum UserRole
    {
        Member = 1,
        Owner  = 2,
    }

    [DataContract]
    public enum VisualStudioLevel
    {
        [EnumMember]
        None = 0,

        [EnumMember]
        Professional = 1,

        [EnumMember]
        TestManager = 2,
    }
}
