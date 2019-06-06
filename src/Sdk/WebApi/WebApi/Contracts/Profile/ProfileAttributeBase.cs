using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Profile
{
    [DataContract]
    public class ProfileAttributeBase<T> : ITimeStamped, IVersioned, ICloneable
    {
        /// <summary>
        /// The descriptor of the attribute.
        /// </summary>
        [DataMember]
        public AttributeDescriptor Descriptor { get; set; }

        /// <summary>
        /// The value of the attribute.
        /// </summary>
        [DataMember]
        public T Value { get; set; }

        /// <summary>
        /// The time the attribute was last changed.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTimeOffset TimeStamp { get; set; }

        /// <summary>
        /// The revision number of the attribute.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public int Revision { get; set; }

        public object Clone()
        {
            ProfileAttributeBase<T> newProfileAttribute = (ProfileAttributeBase<T>)MemberwiseClone();
            newProfileAttribute.Descriptor = Descriptor != null ? (AttributeDescriptor)Descriptor.Clone() : null;
            newProfileAttribute.Value = Value is ICloneable ? (T)((ICloneable)Value).Clone() : Value;
            return newProfileAttribute;
        }
    }
}
