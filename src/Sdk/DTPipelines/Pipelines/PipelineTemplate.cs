using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineTemplate
    {
        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public PipelineResources Resources
        {
            get
            {
                if (m_resources == null)
                {
                    m_resources = new PipelineResources();
                }
                return m_resources;
            }
        }

        public IList<IVariable> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new List<IVariable>();
                }
                return m_variables;
            }
        }

        public IList<Stage> Stages
        {
            get
            {
                if (m_stages == null)
                {
                    m_stages = new List<Stage>();
                }
                return m_stages;
            }
        }

        public IList<PipelineTrigger> Triggers
        {
            get
            {
                if (m_triggers == null)
                {
                    m_triggers = new List<PipelineTrigger>();
                }
                return m_triggers;
            }
        }

        public IList<PipelineValidationError> Errors
        {
            get
            {
                if (m_errors == null)
                {
                    m_errors = new List<PipelineValidationError>();
                }
                return m_errors;
            }
        }

        public IList<PipelineSchedule> Schedules
        {
            get
            {
                if (m_schedules == null)
                {
                    m_schedules = new List<PipelineSchedule>();
                }
                return m_schedules;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public String InitializationLog
        {
            get;
            set;
        }

        public void CheckErrors()
        {
            if (m_errors?.Count > 0)
            {
                throw new PipelineValidationException(m_errors);
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_stages?.Count == 0)
            {
                m_stages = null;
            }

            if (m_errors?.Count == 0)
            {
                m_errors = null;
            }

            if (m_triggers?.Count == 0)
            {
                m_triggers = null;
            }

            if (m_resources?.Count == 0)
            {
                m_resources = null;
            }

            if (m_variables?.Count == 0)
            {
                m_variables = null;
            }

            if (m_schedules?.Count == 0)
            {
                m_schedules = null;
            }
        }

        [DataMember(Name = "Stages", EmitDefaultValue = false)]
        private List<Stage> m_stages;

        [DataMember(Name = "Errors", EmitDefaultValue = false)]
        private List<PipelineValidationError> m_errors;

        [DataMember(Name = "Triggers", EmitDefaultValue = false)]
        private List<PipelineTrigger> m_triggers;

        [DataMember(Name = "Resources", EmitDefaultValue = false)]
        private PipelineResources m_resources;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private List<IVariable> m_variables;

        [DataMember(Name = "Schedules", EmitDefaultValue = false)]
        private List<PipelineSchedule> m_schedules;
    }
}
