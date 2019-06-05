using System;
using System.Runtime.Serialization;

namespace GitHub.Services.UserMapping
{
    [Flags, DataContract]
    public enum UserType
    {
        Member = 1,
        Owner  = 2,
    }
}
