using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Deployment pool summary.
    /// </summary>
    [DataContract]
    public sealed class DeploymentPoolSummary
    {
        /// <summary>
        /// Deployment pool.
        /// </summary>
        [DataMember]
        public TaskAgentPoolReference Pool
        {
            get;
            internal set;
        }

        /// <summary>
        /// Number of deployment agents that are online.
        /// </summary>
        [DataMember]
        public Int32 OnlineAgentsCount
        {
            get;
            internal set;
        }

        /// <summary>
        /// Number of deployment agents that are offline.
        /// </summary>
        [DataMember]
        public Int32 OfflineAgentsCount
        {
            get;
            internal set;
        }

        /// <summary>
        /// Virtual machine Resource referring in pool.
        /// </summary>
        [DataMember]
        public EnvironmentResourceReference Resource
        {
            get;
            internal set;
        }

        /// <summary>
        /// List of deployment groups referring to the deployment pool.
        /// </summary>
        public IList<DeploymentGroupReference> DeploymentGroups
        {
            get
            {
                if (m_deploymentGroups == null)
                {
                    m_deploymentGroups = new List<DeploymentGroupReference>();
                }
                return m_deploymentGroups;
            }
            internal set
            {
                m_deploymentGroups = value;
            }
        }

        /// <summary>
        /// List of deployment groups referring to the deployment pool.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "DeploymentGroups")]
        private IList<DeploymentGroupReference> m_deploymentGroups;
    }
}
