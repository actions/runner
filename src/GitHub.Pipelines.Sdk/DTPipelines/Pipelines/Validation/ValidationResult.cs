using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.Pipelines.Validation
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class ValidationResult
    {
        public PipelineEnvironment Environment
        {
            get;
            internal set;
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

        public PipelineResources ReferencedResources
        {
            get
            {
                if (m_referencedResources == null)
                {
                    m_referencedResources = new PipelineResources();
                }
                return m_referencedResources;
            }
        }

        public PipelineResources UnauthorizedResources
        {
            get
            {
                if (m_unauthorizedResources == null)
                {
                    m_unauthorizedResources = new PipelineResources();
                }
                return m_unauthorizedResources;
            }
        }

        internal void AddQueueReference(
            Int32 id, 
            String name)
        {
            if (id != 0)
            {
                this.ReferencedResources.Queues.Add(new AgentQueueReference { Id = id });
            }
            else if (!String.IsNullOrEmpty(name))
            {
                this.ReferencedResources.Queues.Add(new AgentQueueReference { Name = name });
            }
        }

        internal void AddPoolReference(
            Int32 id,
            String name)
        {
            if (id != 0)
            {
                this.ReferencedResources.Pools.Add(new AgentPoolReference { Id = id });
            }
            else if (!String.IsNullOrEmpty(name))
            {
                this.ReferencedResources.Pools.Add(new AgentPoolReference { Name = name });
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_errors?.Count == 0)
            {
                m_errors = null;
            }

            if (m_referencedResources?.Count == 0)
            {
                m_referencedResources = null;
            }

            if (m_unauthorizedResources?.Count == 0)
            {
                m_unauthorizedResources = null;
            }
        }

        [DataMember(Name = "Errors", EmitDefaultValue = false)]
        private List<PipelineValidationError> m_errors;

        [DataMember(Name = "ReferencedResources", EmitDefaultValue = false)]
        private PipelineResources m_referencedResources;

        [DataMember(Name = "UnauthorizedResources", EmitDefaultValue = false)]
        private PipelineResources m_unauthorizedResources;
    }
}
