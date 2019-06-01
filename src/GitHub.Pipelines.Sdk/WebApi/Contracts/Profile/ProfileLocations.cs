using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Profile
{

    /// <summary>
    /// Types of Profile pages
    /// </summary>
    [DataContract]
    public enum ProfilePageType
    {
        Update,
        Create,
        CreateIDE,
        Review,
        AvatarImageFormat
    }
}