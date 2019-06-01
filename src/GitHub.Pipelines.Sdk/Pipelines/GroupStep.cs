using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class GroupStep : JobStep
    {
        [JsonConstructor]
        public GroupStep()
        {
        }

        private GroupStep(GroupStep groupStepToClone)
            : base(groupStepToClone)
        {
            if (groupStepToClone.m_steps?.Count > 0)
            {
                foreach (var step in groupStepToClone.m_steps)
                {
                    this.Steps.Add(step.Clone() as TaskStep);
                }
            }

            if (groupStepToClone.m_outputs?.Count > 0)
            {
                this.m_outputs = new Dictionary<String, String>(groupStepToClone.m_outputs, StringComparer.OrdinalIgnoreCase);
            }
        }

        public override StepType Type => StepType.Group;

        public IList<TaskStep> Steps
        {
            get
            {
                if (m_steps == null)
                {
                    m_steps = new List<TaskStep>();
                }
                return m_steps;
            }
        }

        public IDictionary<String, String> Outputs
        {
            get
            {
                if (m_outputs == null)
                {
                    m_outputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_outputs;
            }
        }

        public override Step Clone()
        {
            return new GroupStep(this);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_steps?.Count == 0)
            {
                m_steps = null;
            }

            if (m_outputs?.Count == 0)
            {
                m_outputs = null;
            }
        }

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private IList<TaskStep> m_steps;

        [DataMember(Name = "Outputs", EmitDefaultValue = false)]
        private IDictionary<String, String> m_outputs;
    }
}
