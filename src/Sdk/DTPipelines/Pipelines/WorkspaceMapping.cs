using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class WorkspaceMapping
    {
        /// <summary>
        /// The map/cloak in tfvc.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean Exclude
        {
            get;
            set;
        }

        /// <summary>
        /// The server path.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String ServerPath
        {
            get;
            set;
        }

        /// <summary>
        /// The local path.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String LocalPath
        {
            get;
            set;
        }

        /// <summary>
        /// The revision in svn.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Revision
        {
            get;
            set;
        }

        /// <summary>
        /// The depth in svn.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 Depth
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether to ignore externals in svn.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean IgnoreExternals
        {
            get;
            set;
        }
    }
}
