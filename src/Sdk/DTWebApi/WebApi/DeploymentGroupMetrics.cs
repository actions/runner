using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Deployment group metrics.
    /// </summary>
    [DataContract]
    public sealed class DeploymentGroupMetrics
    {
        /// <summary>
        /// Deployment group.
        /// </summary>
        [DataMember]
        public DeploymentGroupReference DeploymentGroup
        {
            get;
            internal set;
        }

        /// <summary>
        /// List of deployment group properties. And types of metrics provided for those properties.
        /// </summary>
        [DataMember]
        public MetricsColumnsHeader ColumnsHeader
        {
            get;
            internal set;
        }

        /// <summary>
        /// Values of properties and the metrics.
        /// E.g. 1: total count of deployment targets for which 'TargetState' is 'offline'.
        /// E.g. 2: Average time of deployment to the deployment targets for which 'LastJobStatus' is 'passed' and 'TargetState' is 'online'.
        /// </summary>
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

        /// <summary>
        /// Values of properties and the metrics.
        /// E.g. 1: total count of deployment targets for which 'TargetState' is 'offline'.
        /// E.g. 2: Average time of deployment to the deployment targets for which 'LastJobStatus' is 'passed' and 'TargetState' is 'online'.
        /// </summary>
        [DataMember(Name = "Rows")]
        private IList<MetricsRow> m_rows;
    }
}
