using System;
using System.Collections.Generic;
using System.Runtime.Serialization;


namespace GitHub.Services.Invitation
{
    /// <summary>
    /// Invitation Data
    /// </summary>
    [DataContract]
    public class Invitee
    {
        /// <summary>
        /// Type of Invitation
        /// </summary>
        [DataMember]
        public Guid UserId { get; set; }

        [DataMember]
        public InviteeStatus Status { get; set; }

        [DataMember]
        public string Message { get; set; }
    }

    public enum InviteeStatus
    {
        None = 0,
        Success = 1,
        Failed = -1
    }
}
