using System;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskAgentPoolMaintenanceRetentionPolicy
    {
        internal TaskAgentPoolMaintenanceRetentionPolicy()
        { }

        private TaskAgentPoolMaintenanceRetentionPolicy(TaskAgentPoolMaintenanceRetentionPolicy maintenanceRetentionPolicyToBeCloned)
        {
            this.NumberOfHistoryRecordsToKeep = maintenanceRetentionPolicyToBeCloned.NumberOfHistoryRecordsToKeep;
        }

        /// <summary>
        /// Number of records to keep for maintenance job executed with this definition.
        /// </summary>
        [DataMember]
        public Int32 NumberOfHistoryRecordsToKeep
        {
            get
            {
                return m_numberOfHistoryRecordsToKeep;
            }
            internal set
            {
                if (value < 1)
                {
                    m_numberOfHistoryRecordsToKeep = 1;
                }
                else
                {
                    m_numberOfHistoryRecordsToKeep = value;
                }
            }
        }

        public TaskAgentPoolMaintenanceRetentionPolicy Clone()
        {
            return new TaskAgentPoolMaintenanceRetentionPolicy(this);
        }

        private Int32 m_numberOfHistoryRecordsToKeep;
    }
}
