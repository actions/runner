using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents the timeline of a build.
    /// </summary>
    [DataContract]
    public sealed class Timeline : TimelineReference
    {
        internal Timeline()
        {
        }

        internal Timeline(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The process or person that last changed the timeline.
        /// </summary>
        [DataMember]
        public Guid LastChangedBy
        {
            get;
            internal set;
        }

        /// <summary>
        /// The time the timeline was last changed.
        /// </summary>
        [DataMember]
        public DateTime LastChangedOn
        {
            get;
            internal set;
        }

        /// <summary>
        /// The list of records in this timeline.
        /// </summary>
        public List<TimelineRecord> Records
        {
            get
            {
                if (m_records == null)
                {
                    m_records = new List<TimelineRecord>();
                }
                return m_records;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (m_serializedRecords != null && m_serializedRecords.Count > 0)
            {
                m_records = new List<TimelineRecord>(m_serializedRecords);
                m_serializedRecords = null;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_records != null && m_records.Count > 0)
            {
                m_serializedRecords = new List<TimelineRecord>(m_records);
            }
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedRecords = null;
        }

        private List<TimelineRecord> m_records;

        [DataMember(Name = "Records", EmitDefaultValue = false)]
        private List<TimelineRecord> m_serializedRecords;
    }
}
