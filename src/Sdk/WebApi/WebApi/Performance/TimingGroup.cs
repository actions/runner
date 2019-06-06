using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Services.Performance
{
    /// <summary>
    /// A set of secured performance timings all keyed off of the same string
    /// </summary>
    [DataContract]
    public class TimingGroup : BaseSecuredObject
    {
        private TimingGroup()
        {
        }

        public TimingGroup(ISecuredObject securedObject) : base(securedObject)
        {
            Timings = new List<TimingEntry>();
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
        public List<TimingEntry> Timings { get; private set; }
    }
}
