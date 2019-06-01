using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace Microsoft.VisualStudio.Services.Invitation
{
    /// <summary>
    /// Invitation Data
    /// </summary>
    [DataContract]
    public class InvitationData
    {
        /// <summary>
        /// Type of Invitation
        /// </summary>
        [DataMember]
        public InvitationType InvitationType { get; set; } = InvitationType.AccountInvite;

        /// <summary>
        /// Id of the Sender
        /// </summary>
        [DataMember]
        public Guid SenderId { get; set; } = Guid.Empty;


        /// <summary>
        /// User Ids of invitation recipients
        /// </summary>
        [DataMember]
        public List<Invitee> Invitees { get; set; }

        /// <summary>
        /// Invitation Attributes
        /// </summary>
        [DataMember]
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}