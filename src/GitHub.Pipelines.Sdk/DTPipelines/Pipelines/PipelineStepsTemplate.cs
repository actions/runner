using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PipelineStepsTemplate
    {
        public IList<Step> Steps
        {
            get
            {
                if (m_steps == null)
                {
                    m_steps = new List<Step>();
                }
                return m_steps;
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

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_steps?.Count == 0)
            {
                m_steps = null;
            }

            if (m_errors?.Count == 0)
            {
                m_errors = null;
            }
        }

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private List<Step> m_steps;

        [DataMember(Name = "Errors", EmitDefaultValue = false)]
        private List<PipelineValidationError> m_errors;
    }
}
