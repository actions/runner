using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{
    /// <summary>
    /// A named object associated with a profile.
    /// </summary>
    [DataContract]
    public class ProfileAttribute : ProfileAttributeBase<string>
    {
    }
}