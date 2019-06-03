using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class StageAttempt
    {
        internal StageAttempt()
        {
        }

        public StageInstance Stage
        {
            get;
            set;
        }

        public IList<PhaseAttempt> Phases
        {
            get
            {
                if (m_phases == null)
                {
                    m_phases = new List<PhaseAttempt>();
                }
                return m_phases;
            }
        }

        public Timeline Timeline
        {
            get;
            internal set;
        }

        private List<PhaseAttempt> m_phases;
    }
}
