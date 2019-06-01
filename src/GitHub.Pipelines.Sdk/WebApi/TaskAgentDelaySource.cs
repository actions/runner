using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentDelaySource : ICloneable
    {
        public TaskAgentDelaySource(TaskAgentReference taskAgent, IEnumerable<TimeSpan> delays)
        {
            TaskAgent = taskAgent;
            Delays = delays.ToList();
        }

        [DataMember]
        public TaskAgentReference TaskAgent { get; }

        [DataMember]
        public List<TimeSpan> Delays { get; }

        public TimeSpan TotalDelay
        {
            get
            {
                if (!m_delay.HasValue)
                {
                    m_delay = Delays.Aggregate(TimeSpan.Zero, (sum, next) => sum + next);
                }

                return m_delay.Value;
            }
        }

        private TimeSpan? m_delay;

        Object ICloneable.Clone()
        {
            return this.Clone();
        }

        public TaskAgentDelaySource Clone()
        {
            return new TaskAgentDelaySource(TaskAgent, new List<TimeSpan>(Delays));
        }
    }
}
