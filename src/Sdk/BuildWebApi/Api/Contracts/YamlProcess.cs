using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a YAML process.
    /// </summary>
    [DataContract]
    public class YamlProcess : BuildProcess
    {
        public YamlProcess()
            : this(null)
        {
        }

        internal YamlProcess(
            ISecuredObject securedObject)
            : base(ProcessType.Yaml, securedObject)
        {
        }

        /// <summary>
        /// The resources used by the build definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildProcessResources Resources
        {
            get;
            set;
        }

        /// <summary>
        /// The list of errors encountered when reading the YAML definition.
        /// </summary>
        public IList<String> Errors
        {
            get
            {
                if (m_errors == null)
                {
                    m_errors = new List<String>();
                }
                return m_errors;
            }
            set
            {
                m_errors = new List<String>(value);
            }
        }

        /// <summary>
        /// The YAML filename.
        /// </summary>
        [DataMember]
        public String YamlFilename
        {
            get;
            set;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_errors?.Count == 0)
            {
                m_errors = null;
            }
        }

        [DataMember(Name = "Errors", EmitDefaultValue = false)]
        private List<String> m_errors;
    }
}
