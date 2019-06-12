using System;
using System.Collections.Generic;
using System.ComponentModel;
using GitHub.DistributedTask.Pipelines.Runtime;

namespace GitHub.DistributedTask.Pipelines
{
    /// <summary>
    /// Represents the runtime values of a phase which has been expanded for execution.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ExpandPhaseResult
    {
        /// <summary>
        /// Initializes a new <c>ExpandPhaseResult</c> innstance with a default maximum concurrency of 1.
        /// </summary>
        public ExpandPhaseResult()
        {
            this.MaxConcurrency = 1;
        }

        /// <summary>
        /// Gets or sets the execution behavior when an error is encountered.
        /// </summary>
        public Boolean ContinueOnError
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the execution behavior when an error is encountered.
        /// </summary>
        public Boolean FailFast
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the maximum concurrency for the jobs.
        /// </summary>
        public Int32 MaxConcurrency
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of jobs for this phase.
        /// </summary>
        public IList<JobInstance> Jobs
        {
            get
            {
                if (m_jobs == null)
                {
                    m_jobs = new List<JobInstance>();
                }
                return m_jobs;
            }
        }

        private List<JobInstance> m_jobs;
    }
}
