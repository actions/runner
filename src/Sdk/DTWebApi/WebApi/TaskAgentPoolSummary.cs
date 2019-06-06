using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskAgentPoolSummary
    {
        [DataMember]
        public TaskAgentPoolReference Pool
        {
            get;
            internal set;
        }

        [DataMember]
        public MetricsColumnsHeader ColumnsHeader
        {
            get;
            internal set;
        }

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

        public IList<TaskAgentQueue> Queues
        {
            get
            {
                if (m_queues == null)
                {
                    m_queues = new List<TaskAgentQueue>();
                }
                return m_queues;
            }
            internal set
            {
                m_queues = value;
            }
        }

        public IList<MetricsRow> Rows
        {
            get
            {
                if (m_rows == null)
                {
                    m_rows = new List<MetricsRow>();
                }

                return m_rows;
            }
            internal set
            {
                m_rows = value;
            }
        }

        [DataMember(Name = "Rows")]
        private IList<MetricsRow> m_rows;

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "DeploymentGroups")]
        private IList<DeploymentGroupReference> m_deploymentGroups;

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Queues")]
        private IList<TaskAgentQueue> m_queues;
    }
}
