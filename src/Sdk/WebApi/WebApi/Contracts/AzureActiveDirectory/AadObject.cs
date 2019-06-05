using System;
using System.Runtime.Serialization;

namespace GitHub.Services.Aad
{
    /// <summary>
    /// Immutable data transfer object for AAD objects.
    /// </summary>
    [DataContract]
    public abstract class AadObject
    {
        [DataMember] private Guid objectId;
        [DataMember] private string displayName;

        protected AadObject()
        {

        }

        public AadObject(Guid objectId, string displayName)
        {
            this.objectId = objectId;
            this.displayName = displayName;
        }

        public Guid ObjectId
        {
            get { return objectId; }
            set { objectId = value; }
        }

        /// <summary>
        /// This could be null.
        /// </summary>
        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || Equals(obj as AadObject);
        }

        private bool Equals(AadObject other)
        {
            if (other == null)
            {
                return false;
            }

            return Equals(ObjectId, other.ObjectId) && string.Equals(DisplayName, other.DisplayName);
        }

        public override int GetHashCode()
        {
            return this.ObjectId.GetHashCode();
        }
    }
}
