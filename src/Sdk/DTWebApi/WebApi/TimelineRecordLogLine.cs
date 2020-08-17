using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TimelineRecordLogLine
    {
        public TimelineRecordLogLine(String line, long lineNumber)
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

        [DataMember]
        public long LineNumber
        {
            get;
            set;
        }

        public override bool Equals(object obj)
        {
            TimelineRecordLogLine logLine = obj as TimelineRecordLogLine;
            
            if(logLine != null)
            {
                return String.Equals(Line, logLine.Line) 
                       && LineNumber.Equals(logLine.LineNumber);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + (Line?.GetHashCode() ?? 0);
            hash = (hash * 7) + LineNumber.GetHashCode();
            return hash;
        }
    }
}
