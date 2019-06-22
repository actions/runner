using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a revision of a build definition.
    /// </summary>
    [DataContract]
    public class BuildDefinitionRevision
    {
        /// <summary>
        /// The revision number.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Revision
        {
            get;
            set;
        }

        /// <summary>
        /// The name of the definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The identity of the person or process that changed the definition.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false, Order = 30)]
        public IdentityRef ChangedBy
        {
            get;

            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// The date and time that the definition was changed.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime ChangedDate
        {
            get;
            set;
        }

        /// <summary>
        /// The change type (add, edit, delete).
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public AuditAction ChangeType
        {
            get;
            set;
        }

        /// <summary>
        /// The comment associated with the change.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Comment
        {
            get;
            set;
        }

        /// <summary>
        /// A link to the definition at this revision.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DefinitionUrl
        {
            get;
            set;
        }
    }
}
