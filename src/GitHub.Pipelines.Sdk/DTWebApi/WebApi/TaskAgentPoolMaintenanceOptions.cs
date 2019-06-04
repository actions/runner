using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskAgentPoolMaintenanceOptions
    {
        internal TaskAgentPoolMaintenanceOptions()
        {
        }

        private TaskAgentPoolMaintenanceOptions(TaskAgentPoolMaintenanceOptions maintenanceOptionToBeCloned)
        {
            this.WorkingDirectoryExpirationInDays = maintenanceOptionToBeCloned.WorkingDirectoryExpirationInDays;
        }

        /// <summary>
        /// time to consider a System.DefaultWorkingDirectory is stale
        /// </summary>
        [DataMember]
        public Int32 WorkingDirectoryExpirationInDays
        {
            get;
            set;
        }

        public TaskAgentPoolMaintenanceOptions Clone()
        {
            return new TaskAgentPoolMaintenanceOptions(this);
        }
    }
}
