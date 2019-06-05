using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    /// <summary>
    /// A named object associated with a profile.
    /// </summary>
    [DataContract]
    public class ProfileAttribute : ProfileAttributeBase<string>
    {
    }
}
