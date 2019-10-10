using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Common.Contracts;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Represents a build definition.
    /// </summary>
    [DataContract]
    public class BuildDefinition : BuildDefinitionReference
    {
        public BuildDefinition()
        {
            this.JobAuthorizationScope = BuildAuthorizationScope.ProjectCollection;
        }

        /// <summary>
        /// The build number format.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String BuildNumberFormat
        {
            get;
            set;
        }

        /// <summary>
        /// A save-time comment for the definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Comment
        {
            get;
            set;
        }

        /// <summary>
        /// The description.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// The drop location for the definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DropLocation
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
        /// The job execution timeout (in minutes) for builds queued against this definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 JobTimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// The job cancel timeout (in minutes) for builds cancelled by user for this definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 JobCancelTimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether badges are enabled for this definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean BadgeEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// The list of steps for this definition.
        /// </summary>
        [Obsolete]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public List<BuildDefinitionStep> Steps
        {
            get;
        }

        /// <summary>
        /// The build process.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildProcess Process
        {
            get;
            set;
        }

        /// <summary>
        /// A list of build options used by this definition.
        /// </summary>
        public List<BuildOption> Options
        {
            get
            {
                if (m_options == null)
                {
                    m_options = new List<BuildOption>();
                }

                return m_options;
            }
            internal set
            {
                m_options = value;
            }
        }

        /// <summary>
        /// The repository.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildRepository Repository
        {
            get;
            set;
        }

        /// <summary>
        /// The process parameters for this definition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ProcessParameters ProcessParameters
        {
            get;
            set;
        }

        /// <summary>
        /// The list of triggers for this definition.
        /// </summary>
        public List<BuildTrigger> Triggers
        {
            get
            {
                if (m_triggers == null)
                {
                    m_triggers = new List<BuildTrigger>();
                }

                return m_triggers;
            }
            internal set
            {
                m_triggers = value;
            }
        }

        /// <summary>
        /// The variables used by this definition.
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
            internal set
            {
                m_variables = new Dictionary<String, BuildDefinitionVariable>(value, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// The variable groups used by this definition.
        /// </summary>
        public List<VariableGroup> VariableGroups
        {
            get
            {
                if (m_variableGroups == null)
                {
                    m_variableGroups = new List<VariableGroup>();
                }

                return m_variableGroups;
            }
            internal set
            {
                m_variableGroups = value;
            }
        }

        /// <summary>
        /// The list of demands that represents the capabilities required by all agents for this definition.
        /// </summary>
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
            internal set
            {
                m_demands = value;
            }
        }

        /// <summary>
        /// The list of retention policies for this definition.
        /// </summary>
        public List<RetentionPolicy> RetentionRules
        {
            get
            {
                if (m_retentionRules == null)
                {
                    m_retentionRules = new List<RetentionPolicy>();
                }

                return m_retentionRules;
            }
            internal set
            {
                m_retentionRules = value;
            }
        }

        /// <summary>
        /// A collection of properties which may be used to extend the storage fields available
        /// for a given definition.
        /// </summary>
        public PropertiesCollection Properties
        {
            get
            {
                if (m_properties == null)
                {
                    m_properties = new PropertiesCollection();
                }

                return m_properties;
            }
            internal set
            {
                m_properties = value;
            }
        }

        /// <summary>
        /// A collection of tags associated with the build definition.
        /// </summary>
        public List<String> Tags
        {
            get
            {
                if (m_tags == null)
                {
                    m_tags = new List<String>();
                }

                return m_tags;
            }
            internal set
            {
                m_tags = value;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedOptions, ref m_options, true);
            SerializationHelper.Copy(ref m_serializedTriggers, ref m_triggers, true);
            SerializationHelper.Copy(ref m_serializedVariables, ref m_variables, StringComparer.OrdinalIgnoreCase, true);
            SerializationHelper.Copy(ref m_serializedVariableGroups, ref m_variableGroups, true);
            SerializationHelper.Copy(ref m_serializedDemands, ref m_demands, true);
            SerializationHelper.Copy(ref m_serializedRetentionRules, ref m_retentionRules, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_options, ref m_serializedOptions);
            SerializationHelper.Copy(ref m_triggers, ref m_serializedTriggers);
            SerializationHelper.Copy(ref m_variables, ref m_serializedVariables, StringComparer.OrdinalIgnoreCase);
            SerializationHelper.Copy(ref m_variableGroups, ref m_serializedVariableGroups);
            SerializationHelper.Copy(ref m_demands, ref m_serializedDemands);
            SerializationHelper.Copy(ref m_retentionRules, ref m_serializedRetentionRules);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedOptions = null;
            m_serializedTriggers = null;
            m_serializedVariables = null;
            m_serializedVariableGroups = null;
            m_serializedRetentionRules = null;
        }

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        private List<BuildOption> m_serializedOptions;

        [DataMember(Name = "Triggers", EmitDefaultValue = false)]
        private List<BuildTrigger> m_serializedTriggers;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, BuildDefinitionVariable> m_serializedVariables;

        [DataMember(Name = "VariableGroups", EmitDefaultValue = false)]
        private List<VariableGroup> m_serializedVariableGroups;

        [DataMember(Name = "Demands", EmitDefaultValue = false)]
        private List<Demand> m_serializedDemands;

        [DataMember(Name = "RetentionRules", EmitDefaultValue = false)]
        private List<RetentionPolicy> m_serializedRetentionRules;

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Properties")]
        private PropertiesCollection m_properties;

        [DataMember(EmitDefaultValue = false, Name = "Tags")]
        private List<String> m_tags;

        private List<Demand> m_demands;
        private List<BuildOption> m_options;
        private List<BuildTrigger> m_triggers;
        private List<RetentionPolicy> m_retentionRules;
        private List<VariableGroup> m_variableGroups;
        private IDictionary<String, BuildDefinitionVariable> m_variables;
    }
}
