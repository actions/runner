using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Metrics columns header
    /// </summary>
    [DataContract]
    public sealed class MetricsColumnsHeader
    {
        /// <summary>
        /// Properties of deployment group for which metrics are provided.
        /// E.g. 1: LastJobStatus
        /// E.g. 2: TargetState
        /// </summary>
        public IList<MetricsColumnMetaData> Dimensions
        {
            get
            {
                if (m_dimensions == null)
                {
                    m_dimensions = new List<MetricsColumnMetaData>();
                }

                return m_dimensions;
            }
            internal set
            {
                m_dimensions = value;
            }
        }

        /// <summary>
        /// The types of metrics. 
        /// E.g. 1: total count of deployment targets.
        /// E.g. 2: Average time of deployment to the deployment targets.
        /// </summary>
        public IList<MetricsColumnMetaData> Metrics
        {
            get
            {
                if (m_metrics == null)
                {
                    m_metrics = new List<MetricsColumnMetaData>();
                }
                
                return m_metrics;
            }
            internal set
            {
                m_metrics = value;
            }
        }

        /// <summary>
        /// Properties of deployment group for which metrics are provided.
        /// E.g. 1: LastJobStatus
        /// E.g. 2: TargetState
        /// </summary>
        [DataMember(Name = "Dimensions")]
        private IList<MetricsColumnMetaData> m_dimensions;

        /// <summary>
        /// The types of metrics. 
        /// E.g. 1: total count of deployment targets.
        /// E.g. 2: Average time of deployment to the deployment targets.
        /// </summary>
        [DataMember(Name = "Metrics")]
        private IList<MetricsColumnMetaData> m_metrics;
    }

    /// <summary>
    /// Meta data for a metrics column.
    /// </summary>
    [DataContract]
    public sealed class MetricsColumnMetaData
    {
        /// <summary>
        /// Name.
        /// </summary>
        [DataMember]
        public String ColumnName
        {
            get;
            internal set;
        }
           
        /// <summary>
        /// Data type.
        /// </summary>
        [DataMember]
        public String ColumnValueType
        {
            get;
            internal set;
        }
    }
}
