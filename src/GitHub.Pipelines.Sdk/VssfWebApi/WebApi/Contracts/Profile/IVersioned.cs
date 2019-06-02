using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{
    public interface IVersioned
    {
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        int Revision { get; }
    }
}