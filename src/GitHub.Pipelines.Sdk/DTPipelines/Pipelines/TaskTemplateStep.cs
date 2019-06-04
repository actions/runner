using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TaskTemplateStep : Step
    {
        public TaskTemplateStep()
        {
        }

        private TaskTemplateStep(TaskTemplateStep templateToClone)
            : base(templateToClone)
        {
            this.Reference = templateToClone.Reference?.Clone();

            if (templateToClone.m_parameters?.Count > 0)
            {
                m_parameters = new Dictionary<String, String>(templateToClone.m_parameters, StringComparer.OrdinalIgnoreCase);
            }
        }

        public override StepType Type => StepType.TaskTemplate;

        public IDictionary<String, String> Parameters
        {
            get
            {
                if (m_parameters == null)
                {
                    m_parameters = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_parameters;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskTemplateReference Reference
        {
            get;
            set;
        }

        public override Step Clone()
        {
            return new TaskTemplateStep(this);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_parameters?.Count == 0)
            {
                m_parameters = null;
            }
        }

        [DataMember(Name = "Parameters", EmitDefaultValue = false)]
        private IDictionary<String, String> m_parameters;
    }
}
