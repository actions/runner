using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.Services.Common
{
    /// <summary>
    /// A set of performance timings all keyed off of the same string
    /// </summary>
    [DataContract]
    public class PerformanceTimingGroup
    {
        public PerformanceTimingGroup()
        {
            this.Timings = new List<PerformanceTimingEntry>();
        }

        /// <summary>
        /// Overall duration of all entries in this group in ticks
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public long ElapsedTicks { get; set; }

        /// <summary>
        /// The total number of timing entries associated with this group
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int Count { get; set; }

        /// <summary>
        /// A list of timing entries in this group. Only the first few entries in each group are collected.
        /// </summary>
        [DataMember]
        public List<PerformanceTimingEntry> Timings { get; private set; }
    }

    /// <summary>
    /// A single timing consisting of a duration and start time
    /// </summary>
    [DataContract]
    public struct PerformanceTimingEntry
    {
        /// <summary>
        /// Duration of the entry in ticks
        /// </summary>
        [DataMember]
        public long ElapsedTicks { get; set; }

        /// <summary>
        /// Offset from Server Request Context start time in microseconds
        /// </summary>
        [DataMember]
        public long StartOffset { get; set; }

        /// <summary>
        /// Properties to distinguish timings within the same group or to provide data to send with telemetry
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public IDictionary<String, Object> Properties { get; set; }
    }
}
