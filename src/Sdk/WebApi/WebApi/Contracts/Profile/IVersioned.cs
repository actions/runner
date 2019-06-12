using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    public interface IVersioned
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        int Revision { get; }
    }
}
