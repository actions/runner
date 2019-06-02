using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualStudio.Services.Users
{
    [DataContract]
    public class UserAttribute
    {
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
        public DateTimeOffset LastModified { get; internal set; }

        /// <summary>
        /// The attribute's revision, for change tracking.
        /// </summary>
        [DataMember(IsRequired = false)]
        public Int32 Revision { get; internal set; }

        public static implicit operator SetUserAttributeParameters(UserAttribute attribute)
        {
            return new SetUserAttributeParameters
            {
                Name = attribute.Name,
                Value = attribute.Value,
                LastModified = attribute.LastModified,
                Revision = attribute.Revision
            };
        }
    }
}
