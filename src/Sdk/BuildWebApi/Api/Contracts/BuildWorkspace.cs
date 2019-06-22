using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents an entry in a workspace mapping.
    /// </summary>
    [DataContract]
    public class MappingDetails
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
        /// The mapping type.
        /// </summary>
        [DataMember(Name = "mappingType")]
        public String MappingType
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
    }

    /// <summary>
    /// Represents a workspace mapping.
    /// </summary>
    [DataContract]
    public class BuildWorkspace
    {
        /// <summary>
        /// The list of workspace mapping entries.
        /// </summary>
        public List<MappingDetails> Mappings
        {
            get
            {
                if (m_mappings == null)
                {
                    m_mappings = new List<MappingDetails>();
                }
                return m_mappings;
            }
        }

        [DataMember(Name = "mappings")]
        private List<MappingDetails> m_mappings;
    }
}
