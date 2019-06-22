using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public sealed class Schedule : BaseSecuredObject
    {
        public Schedule()
        {
        }

        internal Schedule(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// Time zone of the build schedule (String representation of the time zone ID)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public String TimeZoneId { get; set; }

        /// <summary>
        /// Local timezone hour to start
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public Int32 StartHours { get; set; }

        /// <summary>
        /// Local timezone minute to start
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public Int32 StartMinutes { get; set; }

        /// <summary>
        /// Days for a build (flags enum for days of the week)
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public ScheduleDays DaysToBuild { get; set; }

        //TODO: We should be able to remove the ScheduleJobId field in tbl_Definition
        /// <summary>
        /// The Job Id of the Scheduled job that will queue the scheduled build.
        /// Since a single trigger can have multiple schedules and we want a single job
        /// to process a single schedule (since each schedule has a list of branches
        /// to build), the schedule itself needs to define the Job Id.
        /// This value will be filled in when a definition is added or updated.  The UI
        /// does not provide it or use it.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public Guid ScheduleJobId { get; set; }

        /// <summary>
        /// Branches that the schedule affects
        /// </summary>
        public List<String> BranchFilters
        {
            get
            {
                if (m_branchFilters == null)
                {
                    m_branchFilters = new List<String>();
                }

                return m_branchFilters;
            }
            internal set
            {
                m_branchFilters = value;
            }
        }

        /// <summary>
        /// Flag to determine if this schedule should only build if the associated
        /// source has been changed.
        /// </summary>
        [DataMember(IsRequired = false, EmitDefaultValue = true)]
        public bool ScheduleOnlyWithChanges { get; set; }

        [DataMember(Name = "BranchFilters", EmitDefaultValue = false)]
        private List<String> m_branchFilters;
    }
}
