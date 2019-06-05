using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Metrics row.
    /// </summary>
    [DataContract]
    public sealed class MetricsRow
    {
        /// <summary>
        /// The values of the properties mentioned as 'Dimensions' in column header.
        /// E.g. 1: For a property 'LastJobStatus' - metrics will be provided for 'passed', 'failed', etc.
        /// E.g. 2: For a property 'TargetState' - metrics will be provided for 'online', 'offline' targets.
        /// </summary>
        public IList<String> Dimensions
        {
            get
            {
                if (m_dimensions == null)
                {
                    m_dimensions = new List<String>();
                }

                return m_dimensions;
            }
            internal set
            {
                m_dimensions = value;
            }
        }

        /// <summary>
        /// Metrics in serialized format.
        /// Should be deserialized based on the data type provided in header.
        /// </summary>
        public IList<String> Metrics
        {
            get
            {
                if (m_metrics == null)
                {
                    m_metrics = new List<String>();
                }

                return m_metrics;
            }
            internal set
            {
                m_metrics = value;
            }
        }

        /// <summary>
        /// The values of the properties mentioned as 'Dimensions' in column header.
        /// E.g. 1: For a property 'LastJobStatus' - metrics will be provided for 'passed', 'failed', etc.
        /// E.g. 2: For a property 'TargetState' - metrics will be provided for 'online', 'offline' targets.
        /// </summary>
        [DataMember(Name = "Dimensions")]
        private IList<String> m_dimensions;

        /// <summary>
        /// Metrics in serialized format.
        /// Should be deserialized based on the data type provided in header.
        /// </summary>
        [DataMember(Name = "Metrics")]
        private IList<String> m_metrics;
    }
}
