using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public sealed class WorkspaceTemplate
    {
        /// <summary>
        /// Uri of the associated definition
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String DefinitionUri
        {
            get;
            set;
        }

        /// <summary>
        /// List of workspace mappings
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public List<WorkspaceMapping> Mappings
        {
            get;
            set;
        }

        /// <summary>
        /// The last time this template was modified
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public DateTime LastModifiedDate
        {
            get;
            set;
        }

        /// <summary>
        /// The identity that last modified this template
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String LastModifiedBy
        {
            get;
            set;
        }

        /// <summary>
        /// Id of the workspace for this template
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        internal Int32 WorkspaceId
        {
            get;
            set;
        }

    }

    /// <summary>
    /// Mapping for a workspace
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public sealed class WorkspaceMapping
    {
        /// <summary>
        /// Server location of the definition
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String ServerItem
        {
            get;
            set;
        }

        /// <summary>
        /// local location of the definition
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String LocalItem
        {
            get;
            set;
        }

        /// <summary>
        /// type of workspace mapping
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public WorkspaceMappingType MappingType
        {
            get;
            set;
        }

        /// <summary>
        /// Depth of this mapping
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Int32 Depth
        {
            get;
            set;
        }

        /// <summary>
        /// Uri of the associated definition
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        internal String DefinitionUri
        {
            get;
            set;
        }

        /// <summary>
        /// Id of the workspace
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        internal Int32 WorkspaceId
        {
            get;
            set;
        }

        public override String ToString()
        {
            return String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                 "[WorkspaceMapping ServerItem={0} LocalItem={1} MappingType={2} Depth={3}]",
                                 ServerItem,
                                 LocalItem,
                                 MappingType,
                                 Depth);
        }
    }
}
