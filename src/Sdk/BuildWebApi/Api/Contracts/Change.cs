using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a change associated with a build.
    /// </summary>
    [DataContract]
    public class Change : BaseSecuredObject
    {
        public Change()
            : this(null)
        {
        }

        internal Change(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The identifier for the change. For a commit, this would be the SHA1. For a TFVC changeset, this would be the changeset ID.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Id
        {
            get;
            set;
        }

        /// <summary>
        /// The description of the change. This might be a commit message or changeset description.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Message
        {
            get;
            set;
        }

        /// <summary>
        /// The type of change. "commit", "changeset", etc.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Type
        {
            get;
            set;
        }

        /// <summary>
        /// The author of the change.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef Author
        {
            get;
            set;
        }

        /// <summary>
        /// The timestamp for the change.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? Timestamp
        {
            get;
            set;
        }

        /// <summary>
        /// The location of the full representation of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri Location
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the message was truncated.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean MessageTruncated
        {
            get;
            set;
        }

        /// <summary>
        /// The location of a user-friendly representation of the resource.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri DisplayUri
        {
            get;
            set;
        }

        /// <summary>
        /// The person or process that pushed the change.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Pusher
        {
            get;
            set;
        }
    }
}
