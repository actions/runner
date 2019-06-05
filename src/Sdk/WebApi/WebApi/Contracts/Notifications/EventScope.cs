using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Notifications
{
    /// <summary>
    /// Defines a scope for an event.  
    /// </summary>
    [DataContract]
    public class EventScope
    {
        /// <summary>
        /// Required: The event specific type of a scope.
        /// </summary>
        [DataMember]
        public String Type { get; set; }

        /// <summary>
        /// Required: This is the identity of the scope for the type.
        /// </summary>
        [DataMember]
        public Guid Id { get; set; }

        /// <summary>
        /// Optional: The display name of the scope
        /// </summary>
        [DataMember]
        public String Name { get; set; }
    }
}



