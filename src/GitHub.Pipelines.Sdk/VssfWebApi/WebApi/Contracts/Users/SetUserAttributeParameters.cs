using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Users
{
    /// <summary>
    /// Used for updating a user's attributes.
    /// </summary>
    [DataContract]
    public class SetUserAttributeParameters
    {
        public SetUserAttributeParameters()
        {
        }

        public SetUserAttributeParameters(String name, String value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// The unique group-prefixed name of the attribute, e.g. "TFS.TimeZone".
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Name { get; set; }

        /// <summary>
        /// The value of the attribute.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String Value { get; set; }

        /// <summary>
        /// The date/time at which the attribute was last modified.
        /// </summary>
        [DataMember(IsRequired = false)]
        internal DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// The attribute's revision, for change tracking.
        /// </summary>
        [DataMember(IsRequired = false)]
        internal Int32 Revision { get; set; }
    }
}
