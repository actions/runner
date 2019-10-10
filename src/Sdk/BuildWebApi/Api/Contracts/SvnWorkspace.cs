using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a Subversion mapping entry.
    /// </summary>
    [DataContract]
    public class SvnMappingDetails
    {
        /// <summary>
        /// The server path.
        /// </summary>
        [DataMember(Name = "serverPath")]
        public String ServerPath
        {
            get;
            set;
        }

        /// <summary>
        /// The local path.
        /// </summary>
        [DataMember(Name = "localPath")]
        public String LocalPath
        {
            get;
            set;
        }

        /// <summary>
        /// The revision.
        /// </summary>
        [DataMember(Name = "revision")]
        public String Revision
        {
            get;
            set;
        }

        /// <summary>
        /// The depth.
        /// </summary>
        [DataMember(Name = "depth")]
        public Int32 Depth
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether to ignore externals.
        /// </summary>
        [DataMember(Name = "ignoreExternals")]
        public bool IgnoreExternals
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents a subversion workspace.
    /// </summary>
    [DataContract]
    public class SvnWorkspace
    {
        /// <summary>
        /// The list of mappings.
        /// </summary>
        public List<SvnMappingDetails> Mappings
        {
            get
            {
                if (m_Mappings == null)
                {
                    m_Mappings = new List<SvnMappingDetails>();
                }
                return m_Mappings;
            }
        }

        [DataMember(Name = "mappings")]
        private List<SvnMappingDetails> m_Mappings;
    }
}
