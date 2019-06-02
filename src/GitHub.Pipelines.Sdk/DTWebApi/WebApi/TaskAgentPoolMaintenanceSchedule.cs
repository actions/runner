using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskAgentPoolMaintenanceSchedule
    {
        internal TaskAgentPoolMaintenanceSchedule()
        {
            this.DaysToBuild = TaskAgentPoolMaintenanceScheduleDays.None;
        }

        private TaskAgentPoolMaintenanceSchedule(TaskAgentPoolMaintenanceSchedule maintenanceScheduleToBeCloned)
        {
            this.ScheduleJobId = maintenanceScheduleToBeCloned.ScheduleJobId;
            this.StartHours = maintenanceScheduleToBeCloned.StartHours;
            this.StartMinutes = maintenanceScheduleToBeCloned.StartMinutes;
            this.TimeZoneId = maintenanceScheduleToBeCloned.TimeZoneId;
            this.DaysToBuild = maintenanceScheduleToBeCloned.DaysToBuild;
        }

        /// <summary>
        /// The Job Id of the Scheduled job that will queue the pool maintenance job.
        /// </summary>
        [DataMember]
        public Guid ScheduleJobId { get; set; }

        /// <summary>
        /// Time zone of the build schedule (string representation of the time zone id)
        /// </summary>
        [DataMember]
        public String TimeZoneId { get; set; }

        /// <summary>
        /// Local timezone hour to start
        /// </summary>
        [DataMember]
        public Int32 StartHours { get; set; }

        /// <summary>
        /// Local timezone minute to start
        /// </summary>
        [DataMember]
        public Int32 StartMinutes { get; set; }

        /// <summary>
        /// Days for a build (flags enum for days of the week)
        /// </summary>
        [DataMember]
        public TaskAgentPoolMaintenanceScheduleDays DaysToBuild { get; set; }

        public TaskAgentPoolMaintenanceSchedule Clone()
        {
            return new TaskAgentPoolMaintenanceSchedule(this);
        }
    }
}
