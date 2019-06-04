using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Invitation
{
    /// <summary>
    /// User invitation response
    /// </summary>
    [DataContract]
    public class SendInvitationsResponse
    {
        [DataMember]
        public InvitationType InvitationType { get; set; }

        /// <summary>
        /// Batch users' invitation response
        /// </summary>
        [DataMember]
        public List<Invitee> Invitees { get; set; }

        public SendInvitationsResponse()
        {
            Invitees = new List<Invitee>();
        }

    }
}
