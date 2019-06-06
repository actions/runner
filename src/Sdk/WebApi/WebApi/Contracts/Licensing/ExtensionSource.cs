using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Licensing
{
    /// <summary>
    /// Model for assigning an extension to users, used for the Web API
    /// </summary>
    [DataContract]
    public class ExtensionSource : IEquatable<ExtensionSource>
    {
        /// <summary>
        /// extension Identifier
        /// </summary>
        [DataMember]
        public string ExtensionGalleryId { get; set; }

        /// <summary>
        /// The licensing source of the extension. Account, Msdn, ect.
        /// </summary>
        [DataMember]
        public LicensingSource LicensingSource { get; set; }

        /// <summary>
        /// Assignment Source
        /// </summary>
        [DataMember]
        public AssignmentSource AssignmentSource { get; set; }

        public bool Equals(ExtensionSource other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(ExtensionGalleryId, other.ExtensionGalleryId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtensionSource) obj);
        }

        public override int GetHashCode()
        {
            return ExtensionGalleryId.GetHashCode();
        }
    }
}
