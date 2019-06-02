using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskOrchestrationJob : TaskOrchestrationItem
    {
        internal TaskOrchestrationJob()
            : base(TaskOrchestrationItemType.Job)
        {
            this.ExecutionMode = JobExecutionModeTypes.Agent;
        }

        public TaskOrchestrationJob(
            Guid instanceId,
            String name,
            String refName,
            string executionMode = JobExecutionModeTypes.Agent)
            : base(TaskOrchestrationItemType.Job)
        {
            this.InstanceId = instanceId;
            this.Name = name;
            this.RefName = refName;
            this.ExecutionMode = executionMode;
        }

        private TaskOrchestrationJob(TaskOrchestrationJob jobToBeCloned)
            : base(jobToBeCloned.ItemType)
        {
            this.InstanceId = jobToBeCloned.InstanceId;
            this.Name = jobToBeCloned.Name;
            this.RefName = jobToBeCloned.RefName;
            this.ExecutionMode = jobToBeCloned.ExecutionMode;
            this.ExecutionTimeout = jobToBeCloned.ExecutionTimeout;

            if (jobToBeCloned.ExecuteAs != null)
            {
                this.ExecuteAs = new IdentityRef
                {
                    DisplayName = jobToBeCloned.ExecuteAs.DisplayName,
                    Id = jobToBeCloned.ExecuteAs.Id,
                    ImageUrl = jobToBeCloned.ExecuteAs.ImageUrl,
                    IsAadIdentity = jobToBeCloned.ExecuteAs.IsAadIdentity,
                    IsContainer = jobToBeCloned.ExecuteAs.IsContainer,
                    ProfileUrl = jobToBeCloned.ExecuteAs.ProfileUrl,
                    UniqueName = jobToBeCloned.ExecuteAs.UniqueName,
                    Url = jobToBeCloned.ExecuteAs.Url,
                };
            }

            if (jobToBeCloned.m_demands != null)
            {
                m_demands = jobToBeCloned.Demands.Select(x => x.Clone()).ToList();
            }

            if (jobToBeCloned.m_variables != null)
            {
                m_variables = new Dictionary<String, String>(jobToBeCloned.m_variables, StringComparer.OrdinalIgnoreCase);
            }

            if (jobToBeCloned.m_tasks != null)
            {
                m_tasks = jobToBeCloned.m_tasks.Select(x => (TaskInstance)x.Clone()).ToList();
            }
        }

        [DataMember]
        public Guid InstanceId
        {
            get;
            set;
        }

        [DataMember]
        public String Name
        {
            get;
            set;
        }

        [DataMember]
        public String RefName
        {
            get;
            set;
        }

        [DataMember]
        public string ExecutionMode
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public IdentityRef ExecuteAs
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TimeSpan? ExecutionTimeout
        {
            get;
            set;
        }

        public List<Demand> Demands
        {
            get
            {
                if (m_demands == null)
                {
                    m_demands = new List<Demand>();
                }

                return m_demands;
            }
        }

        public IDictionary<String, String> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }

                return m_variables;
            }
        }

        public List<TaskInstance> Tasks
        {
            get
            {
                if (m_tasks == null)
                {
                    m_tasks = new List<TaskInstance>();
                }

                return m_tasks;
            }
        }

        public TaskOrchestrationJob Clone()
        {
            return new TaskOrchestrationJob(this);
        }

        [DataMember(Name = "Demands", EmitDefaultValue = false)]
        private List<Demand> m_demands;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, String> m_variables;

        [DataMember(Name = "Tasks", EmitDefaultValue = false)]
        private List<TaskInstance> m_tasks;
    }
}
