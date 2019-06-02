using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Invitation
{
    /// <summary>
    /// Enum value indicating type of invitation
    /// </summary>
    [DataContract]
    public enum InvitationType
    {
        /// <summary>
        /// Send invitation to organization
        /// </summary>
        AccountInvite = 1,
        /// <summary>
        /// Send invitation to Directory
        /// </summary>
        DirectoryInvite = 2
    }
}