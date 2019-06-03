using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Pipelines;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class PlanEnvironment : IOrchestrationEnvironment
    {
        public PlanEnvironment()
        {
        }

        private PlanEnvironment(PlanEnvironment environmentToClone)
        {
            if (environmentToClone.m_options != null)
            {
                m_options = m_options.ToDictionary(x => x.Key, x => x.Value.Clone());
            }

            if (environmentToClone.m_maskHints != null)
            {
                m_maskHints = environmentToClone.m_maskHints.Select(x => x.Clone()).ToList();
            }

            if (environmentToClone.m_variables != null)
            {
                m_variables = new VariablesDictionary(environmentToClone.m_variables);
            }
        }

        /// <summary>
        /// Gets the collection of mask hints
        /// </summary>
        public List<MaskHint> MaskHints
        {
            get
            {
                if (m_maskHints == null)
                {
                    m_maskHints = new List<MaskHint>();
                }
                return m_maskHints;
            }
        }

        /// <summary>
        /// Gets the collection of options associated with the current context.
        /// </summary>
        /// <remarks>This is being deprecated and should not be used</remarks>
        public IDictionary<Guid, JobOption> Options
        {
            get
            {
                if (m_options == null)
                {
                    m_options = new Dictionary<Guid, JobOption>();
                }
                return m_options;
            }
        }

        /// <summary>
        /// Gets the collection of variables associated with the current context.
        /// </summary>
        public IDictionary<String, String> Variables
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

        OrchestrationProcessType IOrchestrationEnvironment.ProcessType
        {
            get
            {
                return OrchestrationProcessType.Container;
            }
        }

        IDictionary<String, VariableValue> IOrchestrationEnvironment.Variables
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

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedOptions, ref m_options, true);
            SerializationHelper.Copy(ref m_serializedMaskHints, ref m_maskHints, true);

            var secretNames = new HashSet<String>(m_maskHints?.Where(x => x.Type == MaskType.Variable).Select(x => x.Value) ?? new String[0], StringComparer.OrdinalIgnoreCase);
            if (m_serializedVariables != null && m_serializedVariables.Count > 0)
            {
                m_variables = new VariablesDictionary();
                foreach (var variable in m_serializedVariables)
                {
                    m_variables[variable.Key] = new VariableValue(variable.Value, secretNames.Contains(variable.Key));
                }
            }

            m_serializedVariables = null;
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedOptions = null;
            m_serializedMaskHints = null;
            m_serializedVariables = null;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_options, ref m_serializedOptions);
            SerializationHelper.Copy(ref m_maskHints, ref m_serializedMaskHints);

            if (m_variables != null && m_variables.Count > 0)
            {
                m_serializedVariables = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (var variable in m_variables)
                {
                    m_serializedVariables[variable.Key] = variable.Value?.Value;
                }
            }
        }

        private List<MaskHint> m_maskHints;
        private Dictionary<Guid, JobOption> m_options;
        private VariablesDictionary m_variables;

        [DataMember(Name = "Mask", EmitDefaultValue = false)]
        private List<MaskHint> m_serializedMaskHints;

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        private Dictionary<Guid, JobOption> m_serializedOptions;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, String> m_serializedVariables;
    }
}
