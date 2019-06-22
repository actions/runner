using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    // TODO: remove this before dev16 ships. leaving it in for the dev15 cycle to avoid any issues
    [Obsolete("This contract is not used by any product code")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public sealed class PropertyValue
    {
        /// <summary>
        /// Name in the name value mapping
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String PropertyName { get; set; }

        /// <summary>
        /// Value in the name value mapping
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Object Value { get; set; }

        /// <summary>
        /// Guid of identity that changed this property value
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid? ChangedBy { get; set; }

        /// <summary>
        /// The date this property value was changed
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime? ChangedDate { get; set; }
    }
}
