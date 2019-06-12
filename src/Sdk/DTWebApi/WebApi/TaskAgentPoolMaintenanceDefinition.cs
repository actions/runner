using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentPoolMaintenanceDefinition
    {
        internal TaskAgentPoolMaintenanceDefinition()
        {
        }

        private TaskAgentPoolMaintenanceDefinition(TaskAgentPoolMaintenanceDefinition maintenanceDefinitionToBeCloned)
        {
            this.Enabled = maintenanceDefinitionToBeCloned.Enabled;
            this.JobTimeoutInMinutes = maintenanceDefinitionToBeCloned.JobTimeoutInMinutes;
            this.MaxConcurrentAgentsPercentage = maintenanceDefinitionToBeCloned.MaxConcurrentAgentsPercentage;

            if (maintenanceDefinitionToBeCloned.Pool != null)
            {
                this.Pool = new TaskAgentPoolReference
                {
                    Id = maintenanceDefinitionToBeCloned.Pool.Id,
                    Name = maintenanceDefinitionToBeCloned.Pool.Name,
                    Scope = maintenanceDefinitionToBeCloned.Pool.Scope,
                    PoolType = maintenanceDefinitionToBeCloned.Pool.PoolType
                };
            }

            this.m_options = maintenanceDefinitionToBeCloned.Options.Clone();
            this.m_retentionPolicy = maintenanceDefinitionToBeCloned.RetentionPolicy.Clone();
            this.m_scheduleSetting = maintenanceDefinitionToBeCloned.ScheduleSetting.Clone();
        }

        /// <summary>
        /// Id
        /// </summary>
        [DataMember]
        public Int32 Id
        {
            get;
            internal set;
        }

        /// <summary>
        /// Pool reference for the maintenance definition
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPoolReference Pool
        {
            get;
            set;
        }

        /// <summary>
        /// Enable maintenance
        /// </summary>
        [DataMember]
        public Boolean Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Maintenance job timeout per agent
        /// </summary>
        [DataMember]
        public Int32 JobTimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// Max percentage of agents within a pool running maintenance job at given time
        /// </summary>
        [DataMember]
        public Int32 MaxConcurrentAgentsPercentage
        {
            get;
            set;
        }

        /// <summary>
        /// Maintenance option for the definition
        /// </summary>
        public TaskAgentPoolMaintenanceOptions Options
        {
            get
            {
                if (m_options == null)
                {
                    m_options = new TaskAgentPoolMaintenanceOptions()
                    {
                        WorkingDirectoryExpirationInDays = 0,
                    };
                }

                return m_options;
            }
            internal set
            {
                m_options = value;
            }
        }

        /// <summary>
        /// The retention setting for the pool maintenance definition.
        /// </summary>
        public TaskAgentPoolMaintenanceRetentionPolicy RetentionPolicy
        {
            get
            {
                if (m_retentionPolicy == null)
                {
                    m_retentionPolicy = new TaskAgentPoolMaintenanceRetentionPolicy()
                    {
                        NumberOfHistoryRecordsToKeep = 1,
                    };
                }

                return m_retentionPolicy;
            }
            internal set
            {
                m_retentionPolicy = value;
            }
        }

        /// <summary>
        /// The schedule setting for the pool maintenance job.
        /// </summary>
        public TaskAgentPoolMaintenanceSchedule ScheduleSetting
        {
            get
            {
                if (m_scheduleSetting == null)
                {
                    m_scheduleSetting = new TaskAgentPoolMaintenanceSchedule()
                    {
                        DaysToBuild = TaskAgentPoolMaintenanceScheduleDays.None,
                    };
                }

                return m_scheduleSetting;
            }
            internal set
            {
                m_scheduleSetting = value;
            }
        }

        public TaskAgentPoolMaintenanceDefinition Clone()
        {
            return new TaskAgentPoolMaintenanceDefinition(this);
        }

        [DataMember(EmitDefaultValue = false, Name = "Options")]
        public TaskAgentPoolMaintenanceOptions m_options;

        [DataMember(EmitDefaultValue = false, Name = "RetentionPolicy")]
        private TaskAgentPoolMaintenanceRetentionPolicy m_retentionPolicy;

        [DataMember(EmitDefaultValue = false, Name = "ScheduleSetting")]
        private TaskAgentPoolMaintenanceSchedule m_scheduleSetting;
    }
}
