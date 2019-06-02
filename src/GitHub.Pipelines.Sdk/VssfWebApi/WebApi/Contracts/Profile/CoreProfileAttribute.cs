using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{
    /// <summary>
    /// A profile attribute which always has a value for each profile.
    /// </summary>
    [DataContract]
    public class CoreProfileAttribute : ProfileAttributeBase<object>
    {
    }
}