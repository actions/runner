using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskAgentJob
    {
        public TaskAgentJob(
            Guid id,
            String name,
            String container,
            IList<TaskAgentJobStep> steps,
            IDictionary<String, String> sidecarContainers,
            IList<TaskAgentJobVariable> variables)
        {
            this.Id = id;
            this.Name = name;
            this.Container = container;

            m_variables = new List<TaskAgentJobVariable>(variables);
            m_steps = new List<TaskAgentJobStep>(steps);

            if (sidecarContainers?.Count > 0)
            {
                m_sidecarContainers = new Dictionary<String, String>(sidecarContainers, StringComparer.OrdinalIgnoreCase);
            }
        }

        [DataMember]
        public Guid Id
        {
            get;
        }

        [DataMember]
        public String Name
        {
            get;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Container
        {
            get;
        }

        public IList<TaskAgentJobStep> Steps
        {
            get
            {
                if (m_steps == null)
                {
                    m_steps = new List<TaskAgentJobStep>();
                }
                return m_steps;
            }
        }

        public IDictionary<String, String> SidecarContainers
        {
            get
            {
                if (m_sidecarContainers == null)
                {
                    m_sidecarContainers = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_sidecarContainers;
            }
        }

        public IList<TaskAgentJobVariable> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new List<TaskAgentJobVariable>();
                }
                return m_variables;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_sidecarContainers?.Count == 0)
            {
                m_sidecarContainers = null;
            }
        }

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private List<TaskAgentJobStep> m_steps;

        [DataMember(Name = "SidecarContainers", EmitDefaultValue = false)]
        private IDictionary<String, String> m_sidecarContainers;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private List<TaskAgentJobVariable> m_variables;
    }
}
