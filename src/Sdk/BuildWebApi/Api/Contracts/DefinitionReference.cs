using System;
using System.Runtime.Serialization;
using GitHub.Core.WebApi;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a reference to a definition.
    /// </summary>
    [DataContract]
    [KnownType(typeof(BuildDefinition))]
    [KnownType(typeof(BuildDefinitionReference))]
    [JsonConverter(typeof(DefinitionReferenceJsonConverter))]
#pragma warning disable 618
    public class DefinitionReference : ShallowReference, ISecuredObject
#pragma warning restore 618
    {
        /// <summary>
        /// The ID of the referenced definition.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public new Int32 Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }

        /// <summary>
        /// The name of the referenced definition.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public new String Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// The REST URL of the definition.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public new String Url
        {
            get
            {
                return base.Url;
            }
            set
            {
                base.Url = value;
            }
        }

        /// <summary>
        /// The definition's URI.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Uri Uri
        {
            get;
            set;
        }

        /// <summary>
        /// The folder path of the definition.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String Path
        {
            get;
            set;
        }

        /// <summary>
        /// The type of the definition.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DefinitionType Type
        {
            get;
            set;
        }

        /// <summary>
        /// A value that indicates whether builds can be queued against this definition.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public DefinitionQueueStatus QueueStatus
        {
            get;
            set;
        }

        /// <summary>
        /// The definition revision number.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32? Revision
        {
            get;
            set;
        }

        /// <summary>
        /// The date this version of the definition was created.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime CreatedDate
        {
            get;
            set;
        }

        /// <summary>
        /// A reference to the project.
        /// </summary>
        [DataMember(EmitDefaultValue = false, Order = 1000)]
        public TeamProjectReference Project
        {
            get;
            set;
        }

        #region ISecuredObject implementation

        Guid ISecuredObject.NamespaceId => Security.BuildNamespaceId;

        Int32 ISecuredObject.RequiredPermissions => m_requiredPermissions;

        String ISecuredObject.GetToken()
        {
            if (!String.IsNullOrEmpty(m_nestingSecurityToken))
            {
                return m_nestingSecurityToken;
            }

            return GetToken(this.Project, this.Path, this.Id);
        }

        internal void SetRequiredPermissions(
            Int32 newValue)
        {
            m_requiredPermissions = newValue;
        }

        internal void SetNestingSecurityToken(
            String tokenValue)
        {
            // For anything more detailed than a DefinitionReference,
            // we don't let you use a nesting security token. 
            if (this is BuildDefinitionReference)
            {
                // Debug.Fail("Nesting security tokens is not allowed for anything more detailed than a DefinitionReference");
                m_nestingSecurityToken = String.Empty;
                return;
            }

            m_nestingSecurityToken = tokenValue;
        }

        internal static String GetToken(
            TeamProjectReference project,
            String path,
            Int32 definitionId)
        {
            return GetToken(project?.Id, path, definitionId);
        }

        internal static String GetToken(
            Guid? projectId,
            String path,
            Int32 definitionId)
        {
            return String.Concat(projectId?.ToString("D") ?? String.Empty, Security.GetSecurityTokenPath(path ?? String.Empty), definitionId);
        }

        private Int32 m_requiredPermissions = BuildPermissions.ViewBuildDefinition;
        private String m_nestingSecurityToken = String.Empty;
        #endregion
    }
}
