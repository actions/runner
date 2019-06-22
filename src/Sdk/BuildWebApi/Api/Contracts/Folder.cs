using System;
using System.Runtime.Serialization;
using GitHub.Core.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a folder that contains build definitions.
    /// </summary>
    [DataContract]
    public class Folder : ISecuredObject
    {
        public Folder()
        {
        }

        /// <summary>
        /// The full path.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Path
        {
            get;
            set;
        }

        /// <summary>
        /// The description.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// The date the folder was created.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime CreatedOn
        {
            get;
            set;
        }

        /// <summary>
        /// The process or person who created the folder.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef CreatedBy
        {
            get;
            set;
        }

        /// <summary>
        /// The date the folder was last changed.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? LastChangedDate
        {
            get;
            set;
        }

        /// <summary>
        /// The process or person that last changed the folder.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IdentityRef LastChangedBy
        {
            get;
            set;
        }

        /// <summary>
        /// The project.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TeamProjectReference Project
        {
            get;
            set;
        }

        #region ISecuredObject implementation

        // We don't have folder-specific permissions. Folders are secured by the project.

        public Guid NamespaceId
        {
            get
            {
                ArgumentUtility.CheckForNull(Project, nameof(Project));
                return ((ISecuredObject)Project).NamespaceId;
            }
        }

        public Int32 RequiredPermissions
        {
            get
            {
                ArgumentUtility.CheckForNull(Project, nameof(Project));
                return ((ISecuredObject)Project).RequiredPermissions;
            }
        }

        public String GetToken()
        {
            ArgumentUtility.CheckForNull(Project, nameof(Project));
            return ((ISecuredObject)Project).GetToken();
        }

        #endregion
    }
}
