using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Deployment group.
    /// </summary>
    [DataContract]
    public class DeploymentGroup : DeploymentGroupReference
    {
        /// <summary>
        /// Number of deployment targets in the deployment group.
        /// </summary>
        [DataMember]
        public Int32 MachineCount
        {
            get;
            internal set;
        }

        /// <summary>
        /// List of deployment targets in the deployment group.
        /// </summary>
        public IList<DeploymentMachine> Machines
        {
            get
            {
                if (m_machines == null)
                {
                    m_machines = new List<DeploymentMachine>();
                }
                return m_machines;
            }
            internal set
            {
                m_machines = value;
            }
        }

        /// <summary>
        /// Description of the deployment group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// List of unique tags across all deployment targets in the deployment group.
        /// </summary>
        public IList<String> MachineTags
        {
            get
            {
                if (m_tags == null)
                {
                    m_tags = new List<String>();
                }
                return m_tags;
            }
            internal set
            {
                m_tags = value;
            }
        }

        /// <summary>
        /// List of deployment targets in the deployment group.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Machines")]
        private IList<DeploymentMachine> m_machines;

        /// <summary>
        /// List of unique tags across all deployment targets in the deployment group.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "MachineTags")]
        private IList<String> m_tags;
    }
}
