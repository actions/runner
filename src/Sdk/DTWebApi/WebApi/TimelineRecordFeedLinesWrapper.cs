using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TimelineRecordFeedLinesWrapper
    {
        public TimelineRecordFeedLinesWrapper()
        {
        }

        public TimelineRecordFeedLinesWrapper(Guid stepId, IList<string> lines)
        {
            this.StepId = stepId;
            this.Value = lines.ToList();
            this.Count = lines.Count;
        }

        public TimelineRecordFeedLinesWrapper(Guid stepId, IList<string> lines, Int64 startLine)
            : this(stepId, lines)
        {
            this.StartLine = startLine;
        }

        [DataMember(Order = 0)]
        public Int32 Count { get; private set; }

        [DataMember]
        public List<string> Value
        {
            get; private set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Guid StepId { get; set; }

        [DataMember (EmitDefaultValue = false)]
        public Int64? StartLine { get; private set; }
    }
}
