using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.WebApi;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class PipelineEnvironment : IOrchestrationEnvironment
    {
        public PipelineEnvironment()
        {
            this.Version = 1;
        }

        /// <summary>
        /// Gets the resources available for use within the environment.
        /// </summary>
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

        /// <summary>
        /// Gets the counter values, by prefix, which have been allocated for this environment.
        /// </summary>
        public IDictionary<String, Int32> Counters
        {
            get
            {
                if (m_counters == null)
                {
                    m_counters = new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase);
                }
                return m_counters;
            }
        }

        /// <summary>
        /// Gets or sets the user variables collection. Variables are applied in order, meaning if variable names
        /// appear more than once the last value will be represented in the environment.
        /// </summary>
        public IList<IVariable> UserVariables
        {
            get
            {
                if (m_userVariables == null)
                {
                    m_userVariables = new List<IVariable>();
                }
                return m_userVariables;
            }
        }

        /// <summary>
        /// Gets the system variables collection. System variables are always applied last in order to enforce 
        /// precedence.
        /// </summary>
        public IDictionary<String, VariableValue> SystemVariables
        {
            get
            {
                if (m_systemVariables == null)
                {
                    m_systemVariables = new VariablesDictionary();
                }
                return m_systemVariables;
            }
        }

        /// <summary>
        /// Gets the explicit variables defined for use within the pipeline.
        /// </summary>
        [Obsolete("This property is obsolete. Use UserVariables and/or SystemVariables instead")]
        public IDictionary<String, VariableValue> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new VariablesDictionary();
                }
                return m_variables;
            }
        }

        /// <summary>
        /// Gets the execution options for this pipeline.
        /// </summary>
        public ExecutionOptions Options
        {
            get
            {
                return m_options;
            }
        }

        /// <summary>
        /// Gets the version of the environment.
        /// </summary>
        [DefaultValue(1)]
        [DataMember(Name = "Version", EmitDefaultValue = false)]
        public Int32 Version
        {
            get;
            set;
        }

        OrchestrationProcessType IOrchestrationEnvironment.ProcessType
        {
            get
            {
                return m_processType;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_resources?.Count == 0)
            {
                m_resources = null;
            }

            if (m_counters?.Count == 0)
            {
                m_counters = null;
            }

            if (m_userVariables?.Count == 0)
            {
                m_userVariables = null;
            }

            if (m_systemVariables?.Count == 0)
            {
                m_systemVariables = null;
            }

            if (m_variables?.Count == 0)
            {
                m_variables = null;
            }
        }

        [DataMember(Name = "Counters", EmitDefaultValue = false)]
        private Dictionary<String, Int32> m_counters;

        [DataMember(Name = "Options")]
        private ExecutionOptions m_options = new ExecutionOptions();

        [DataMember(Name = "ProcessType")]
        private OrchestrationProcessType m_processType = OrchestrationProcessType.Pipeline;

        [DataMember(Name = "Resources", EmitDefaultValue = false)]
        private PipelineResources m_resources;

        [DataMember(Name = "SystemVariables", EmitDefaultValue = false)]
        private VariablesDictionary m_systemVariables;

        [DataMember(Name = "UserVariables", EmitDefaultValue = false)]
        private IList<IVariable> m_userVariables;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private VariablesDictionary m_variables;
    }
}
