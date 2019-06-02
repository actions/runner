using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.UserMapping
{
    [Flags, DataContract]
    public enum UserType
    {
        Member = 1,
        Owner  = 2,
    }
}
