using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Performance
{
    /// <summary>
    /// A single secured timing consisting of a duration and start time
    /// </summary>
    [DataContract]
    public class TimingEntry : BaseSecuredObject
    {
        private TimingEntry()
        {
        }

        public TimingEntry(ISecuredObject securedObject) : base(securedObject)
        {
        }

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
