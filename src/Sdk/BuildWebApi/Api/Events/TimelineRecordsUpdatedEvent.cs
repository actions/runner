using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    public class TimelineRecordsUpdatedEvent : RealtimeBuildEvent
    {
        public TimelineRecordsUpdatedEvent(
            Int32 buildId,
            IEnumerable<TimelineRecord> records)
            : base(buildId)
        {
            this.TimelineRecords = records;
        }

        [DataMember(IsRequired = true)]
        public IEnumerable<TimelineRecord> TimelineRecords
        {
            get;
            private set;
        }
    }
}
