using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a phase of a build definition.
    /// </summary>
    [DataContract]
    public class Phase : BaseSecuredObject
    {
        public Phase()
        {
        }

        internal Phase(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The name of the phase.
        /// </summary>
        [DataMember]
        public String Name
        {
            get;
            set;
        }

        /// <summary>
        /// The unique ref name of the phase.
        /// </summary>
        [DataMember]
        public String RefName
        {
            get;
            set;
        }

        /// <summary>
        /// The list of steps run by the phase.
        /// </summary>
        public List<BuildDefinitionStep> Steps
        {
            get
            {
                if (m_steps == null)
                {
                    m_steps = new List<BuildDefinitionStep>();
                }

                return m_steps;
            }
            set
            {
                m_steps = value;
            }
        }

        /// <summary>
        /// The list of variables defined on the phase.
        /// </summary>
        public IDictionary<String, BuildDefinitionVariable> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new Dictionary<String, BuildDefinitionVariable>(StringComparer.OrdinalIgnoreCase);
                }

                return m_variables;
            }
            set
            {
                m_variables = new Dictionary<String, BuildDefinitionVariable>(value, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// The list of dependencies for this phase.
        /// </summary>
        public List<Dependency> Dependencies
        {
            get
            {
                if (m_dependencies == null)
                {
                    m_dependencies = new List<Dependency>();
                }

                return m_dependencies;
            }
            set
            {
                m_dependencies = value;
            }
        }

        /// <summary>
        /// The condition that must be true for this phase to execute.
        /// </summary>
        /// <remarks>
        /// The condition is evaluated after all dependencies are satisfied.
        /// </remarks>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public String Condition
        {
            get;
            set;
        }

        /// <summary>
        /// The target (agent, server, etc.) for this phase.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public PhaseTarget Target
        {
            get;
            set;
        }

        /// <summary>
        /// The job authorization scope for builds queued against this definition.
        /// </summary>
        [DataMember]
        public BuildAuthorizationScope JobAuthorizationScope
        {
            get;
            set;
        }

        /// <summary>
        /// The job execution timeout, in minutes, for builds queued against this definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 JobTimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// The cancellation timeout, in minutes, for builds queued against this definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 JobCancelTimeoutInMinutes
        {
            get;
            set;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedDependencies, ref m_dependencies, true);
            SerializationHelper.Copy(ref m_serializedSteps, ref m_steps, true);
            SerializationHelper.Copy(ref m_serializedVariables, ref m_variables, StringComparer.OrdinalIgnoreCase, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_dependencies, ref m_serializedDependencies);
            SerializationHelper.Copy(ref m_steps, ref m_serializedSteps);
            SerializationHelper.Copy(ref m_variables, ref m_serializedVariables, StringComparer.OrdinalIgnoreCase);
        }

        [DataMember(Name = "Dependencies", EmitDefaultValue = false)]
        private List<Dependency> m_serializedDependencies;

        [DataMember(Name = "Steps", EmitDefaultValue = false)]
        private List<BuildDefinitionStep> m_serializedSteps;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, BuildDefinitionVariable> m_serializedVariables;

        private List<Dependency> m_dependencies;
        private List<BuildDefinitionStep> m_steps;
        private IDictionary<String, BuildDefinitionVariable> m_variables;
    }
}
