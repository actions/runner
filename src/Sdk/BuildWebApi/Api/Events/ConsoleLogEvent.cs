using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    public sealed class ConsoleLogEvent : RealtimeBuildEvent
    {
        public ConsoleLogEvent(
            Int32 buildId,
            Guid timelineId,
            Guid jobTimelineRecordId,
            IEnumerable<String> lines)
            : this(buildId, timelineId, jobTimelineRecordId, Guid.Empty, lines)
        {
        }

        public ConsoleLogEvent(
            Int32 buildId,
            Guid timelineId,
            Guid jobTimelineRecordId,
            Guid stepTimelineRecordId,
            IEnumerable<String> lines)
            : base(buildId)
        {
            this.TimelineId = timelineId;
            this.TimelineRecordId = jobTimelineRecordId;
            this.StepRecordId = stepTimelineRecordId;
            m_lines = new List<String>(lines);
        }

        [DataMember(IsRequired = true)]
        public Guid TimelineId
        {
            get;
            private set;
        }

        [DataMember(IsRequired = true)]
        public Guid TimelineRecordId
        {
            get;
            private set;
        }

        [DataMember(IsRequired = false)]
        public Guid StepRecordId
        {
            get;
            private set;
        }

        public List<String> Lines
        {
            get
            {
                if (m_lines == null)
                {
                    m_lines = new List<String>();
                }
                return m_lines;
            }
        }

        [DataMember(Name = "Lines")]
        private List<String> m_lines;
    }
}
