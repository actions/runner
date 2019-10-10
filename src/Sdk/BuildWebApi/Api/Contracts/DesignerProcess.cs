using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a build process supported by the build definition designer.
    /// </summary>
    [DataContract]
    public class DesignerProcess : BuildProcess
    {
        public DesignerProcess()
            :this(null)
        {
        }

        internal DesignerProcess(
            ISecuredObject securedObject)
            : base(ProcessType.Designer, securedObject)
        {
        }

        /// <summary>
        /// The list of phases.
        /// </summary>
        public List<Phase> Phases
        {
            get
            {
                if (m_phases == null)
                {
                    m_phases = new List<Phase>();
                }
                return m_phases;
            }
        }

        [DataMember(Name = "Phases", EmitDefaultValue = false)]
        private List<Phase> m_phases;

        /// <summary>
        /// The target for the build process.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DesignerProcessTarget Target { get; set; }
    }
}
