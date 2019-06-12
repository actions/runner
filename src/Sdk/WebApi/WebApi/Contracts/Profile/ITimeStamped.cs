using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    public interface ITimeStamped
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        DateTimeOffset TimeStamp { get; set; }
    }
}
