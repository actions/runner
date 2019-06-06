using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineSchedule
    {
        public PipelineSchedule()
        {
            ScheduleOnlyWithChanges = true; // default schedule only with changes to true, in case it is not encountered in the script
            BatchSchedules = false; // default batch schedules to false, in case it is not encountered in the script
        }

        /// <summary>
        /// A string that will inform us as to what type of schedule we're working with, currently, YAML only supports cron.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String ScheduleType
        {
            get
            {
                return PipelineConstants.ScheduleType.Cron;
            }
        }

        /// <summary>
        /// A string of the schedule details for this schedule. At this point, only cron syntax is supported, so this string will contain the cron syntax input.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String ScheduleDetails
        {
            get;
            set;
        }

        /// <summary>
        /// Display name of the schedule, this will also act as the schedule's name.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// A list of filters that describe which branches will by triggered by this schedule.
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
        /// Indicates whether scheduled runs should be batched together if the previously scheduled run is still running
        /// </summary>
        /// <remarks>
        /// If this is true, then the schedule will wait to trigger if a prior scheduled run is still active, and will trigger the most recent schedule, upon the prior run's completion.
        /// If this is false, then all schedules will be queued to build.
        /// </remarks>
        [DataMember(EmitDefaultValue = false)]
        public Boolean BatchSchedules
        {
            get;
            set;
        }

        /// <summary>
        /// Schedule Job Id
        /// </summary>
        /// <remarks>
        /// The guid used to queue schedule jobs in TFS
        /// </remarks>
        [DataMember(EmitDefaultValue = false)]
        public Guid ScheduleJobId
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the schedule should run regardless of changes to the source version
        /// </summary>
        /// <remarks>
        /// If this is true, then the schedule will run whether there have been changes in the source code or not.
        /// If this is false, then a schedule will only run if there have been changes in the source code since the last scheduled run.
        /// </remarks>
        [DataMember(EmitDefaultValue = false)]
        public Boolean ScheduleOnlyWithChanges
        {
            get;
            set;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_branchFilters?.Count == 0)
            {
                m_branchFilters = null;
            }
        }

        [DataMember(Name = "BranchFilters", EmitDefaultValue = false)]
        private List<String> m_branchFilters;

    }
}
