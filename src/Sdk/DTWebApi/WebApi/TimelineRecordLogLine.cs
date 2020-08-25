using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TimelineRecordLogLine
    {
        public TimelineRecordLogLine(String line, long? lineNumber)
        {
            this.Line = line;
            this.LineNumber = lineNumber;
        }

        [DataMember]
        public String Line
        {
            get;
            set;
        }

        [DataMember (EmitDefaultValue = false)]
        public long? LineNumber
        {
            get;
            set;
        }
    }
}
