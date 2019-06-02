using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ContinuousIntegrationTrigger : PipelineTrigger
    {
        public ContinuousIntegrationTrigger()
            : base(PipelineTriggerType.ContinuousIntegration)
        {
            Enabled = true;
        }

        [DataMember(EmitDefaultValue = true)]
        public Boolean Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether changes should be batched while another CI pipeline is running.
        /// </summary>
        /// <remarks>
        /// If this is true, then changes submitted while a CI pipeline is running will be batched and built in one new CI pipeline when the current pipeline finishes.
        /// If this is false, then a new CI pipeline will be triggered for each change to the repository.
        /// </remarks>
        [DataMember(EmitDefaultValue = false)]
        public Boolean BatchChanges
        {
            get;
            set;
        }

        /// <summary>
        /// A list of filters that describe which branches will trigger pipelines.
        /// </summary>
        public IList<String> BranchFilters
        {
            get
            {
                if (m_branchFilters == null)
                {
                    m_branchFilters = new List<String>();
                }
                return m_branchFilters;
            }
        }

        /// <summary>
        /// A list of filters that describe which paths will trigger pipelines.
        /// </summary>
        public IList<String> PathFilters
        {
            get
            {
                if (m_pathFilters == null)
                {
                    m_pathFilters = new List<String>();
                }
                return m_pathFilters;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_branchFilters?.Count == 0)
            {
                m_branchFilters = null;
            }

            if (m_pathFilters?.Count == 0)
            {
                m_pathFilters = null;
            }
        }

        [DataMember(Name = "BranchFilters", EmitDefaultValue = false)]
        private List<String> m_branchFilters;

        [DataMember(Name = "PathFilters", EmitDefaultValue = false)]
        private List<String> m_pathFilters;
    }
}
