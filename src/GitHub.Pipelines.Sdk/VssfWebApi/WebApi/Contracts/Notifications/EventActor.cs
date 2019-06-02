using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Notifications
{
    /// <summary>
    /// Defines an "actor" for an event.  
    /// </summary>
    [DataContract]
    public class EventActor
    {
        /// <summary>
        /// Required: The event specific name of a role.
        /// </summary>
        [DataMember]
        public String Role { get; set; }

        /// <summary>
        /// Required: This is the identity of the user for the specified role.
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }
    }
}


