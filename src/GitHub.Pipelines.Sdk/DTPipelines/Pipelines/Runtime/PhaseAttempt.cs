using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace GitHub.DistributedTask.Pipelines.Runtime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PhaseAttempt
    {
        public PhaseInstance Phase
        {
            get;
            set;
        }

        public IList<JobAttempt> Jobs
        {
            get
            {
                if (m_jobs == null)
                {
                    m_jobs = new List<JobAttempt>();
                }
                return m_jobs;
            }
        }

        private List<JobAttempt> m_jobs;
    }
}
