using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using GitHub.DistributedTask.Common.Contracts;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi.Internals
{
    /// <summary>
    /// For back-compat with extensions that use the old Steps format instead of Process and Phases
    /// </summary>
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class BuildDefinition3_2 : BuildDefinitionReference3_2
    {
        public BuildDefinition3_2()
        {
            this.JobAuthorizationScope = BuildAuthorizationScope.ProjectCollection;
        }

        /// <summary>
        /// The build number format
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String BuildNumberFormat
        {
            get;
            set;
        }

        /// <summary>
        /// The comment entered when saving the definition
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Comment
        {
            get;
            set;
        }

        /// <summary>
        /// The description
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        /// <summary>
        /// The drop location for the definition
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DropLocation
        {
            get;
            set;
        }

        /// <summary>
        /// The job authorization scope for builds which are queued against this definition
        /// </summary>
        [DataMember]
        public BuildAuthorizationScope JobAuthorizationScope
        {
            get;
            set;
        }

        /// <summary>
        /// The job execution timeout in minutes for builds which are queued against this definition
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 JobTimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// The job cancel timeout in minutes for builds which are cancelled by user for this definition
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int32 JobCancelTimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether badges are enabled for this definition
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
        }

        /// <summary>
        /// Build options
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
        }

        /// <summary>
        /// The repository
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public BuildRepository Repository
        {
            get;
            set;
        }

        /// <summary>
        /// Process Parameters
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public ProcessParameters ProcessParameters
        {
            get;
            set;
        }

        /// <summary>
        ///  The triggers
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
        }

        /// <summary>
        /// The variables.
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
        }

        /// <summary>
        /// The demands.
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
        }

        /// <summary>
        /// The retention rules.
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
        }

        /// <summary>
        /// The latest build for this definition.
        /// </summary>
        public Build LatestBuild
        {
            get
            {
                return m_latestBuild;
            }
            internal set
            {
                m_latestBuild = value;
            }
        }

        /// <summary>
        /// The latest completed build for this definition.
        /// </summary>
        public Build LatestCompletedBuild
        {
            get
            {
                return m_latestCompletedBuild;
            }
            internal set
            {
                m_latestCompletedBuild = value;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedOptions, ref m_options, true);
            SerializationHelper.Copy(ref m_serializedSteps, ref m_steps, true);
            SerializationHelper.Copy(ref m_serializedTriggers, ref m_triggers, true);
            SerializationHelper.Copy(ref m_serializedVariables, ref m_variables, StringComparer.OrdinalIgnoreCase, true);
            SerializationHelper.Copy(ref m_serializedDemands, ref m_demands, true);
            SerializationHelper.Copy(ref m_serializedRetentionRules, ref m_retentionRules, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_options, ref m_serializedOptions);
            SerializationHelper.Copy(ref m_steps, ref m_serializedSteps);
            SerializationHelper.Copy(ref m_triggers, ref m_serializedTriggers);
            SerializationHelper.Copy(ref m_variables, ref m_serializedVariables, StringComparer.OrdinalIgnoreCase);
            SerializationHelper.Copy(ref m_demands, ref m_serializedDemands);
            SerializationHelper.Copy(ref m_retentionRules, ref m_serializedRetentionRules);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedSteps = null;
            m_serializedOptions = null;
            m_serializedTriggers = null;
            m_serializedVariables = null;
            m_serializedRetentionRules = null;
        }

        [DataMember(Name = "Build", EmitDefaultValue = false)]
        private List<BuildDefinitionStep> m_serializedSteps;

        [DataMember(Name = "Options", EmitDefaultValue = false)]
        private List<BuildOption> m_serializedOptions;

        [DataMember(Name = "Triggers", EmitDefaultValue = false)]
        private List<BuildTrigger> m_serializedTriggers;

        [DataMember(Name = "Variables", EmitDefaultValue = false)]
        private IDictionary<String, BuildDefinitionVariable> m_serializedVariables;

        [DataMember(Name = "Demands", EmitDefaultValue = false)]
        private List<Demand> m_serializedDemands;

        [DataMember(Name = "RetentionRules", EmitDefaultValue = false)]
        private List<RetentionPolicy> m_serializedRetentionRules;

        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "Properties")]
        private PropertiesCollection m_properties;

        [DataMember(EmitDefaultValue = false, Name = "Tags")]
        private List<String> m_tags;

        [DataMember(EmitDefaultValue = false, Name = "LatestBuild")]
        private Build m_latestBuild;

        [DataMember(EmitDefaultValue = false, Name = "LatestCompletedBuild")]
        private Build m_latestCompletedBuild;

        private List<Demand> m_demands;
        private List<BuildOption> m_options;
        private List<BuildTrigger> m_triggers;
        private List<RetentionPolicy> m_retentionRules;
        private List<BuildDefinitionStep> m_steps;
        private IDictionary<String, BuildDefinitionVariable> m_variables;
    }

    internal static class BuildDefinition3_2Extensions
    {
        public static BuildDefinition ToBuildDefinition(
            this BuildDefinition3_2 source)
        {
            if (source == null)
            {
                return null;
            }

            var result = new BuildDefinition()
            {
                AuthoredBy = source.AuthoredBy,
                BadgeEnabled = source.BadgeEnabled,
                BuildNumberFormat = source.BuildNumberFormat,
                Comment = source.Comment,
                CreatedDate = source.CreatedDate,
                DefinitionQuality = source.DefinitionQuality,
                Description = source.Description,
                DropLocation = source.DropLocation,
                Id = source.Id,
                JobAuthorizationScope = source.JobAuthorizationScope,
                JobCancelTimeoutInMinutes = source.JobCancelTimeoutInMinutes,
                JobTimeoutInMinutes = source.JobTimeoutInMinutes,
                LatestBuild = source.LatestBuild,
                LatestCompletedBuild = source.LatestCompletedBuild,
                Name = source.Name,
                ParentDefinition = source.ParentDefinition,
                Path = source.Path,
                ProcessParameters = source.ProcessParameters,
                Project = source.Project,
                Queue = source.Queue,
                QueueStatus = source.QueueStatus,
                Repository = source.Repository,
                Revision = source.Revision,
                Type = source.Type,
                Uri = source.Uri,
                Url = source.Url
            };

            if (source.Demands.Count > 0)
            {
                result.Demands.AddRange(source.Demands);
            }

            if (source.Metrics.Count > 0)
            {
                result.Metrics.AddRange(source.Metrics);
            }

            if (source.Options.Count > 0)
            {
                result.Options.AddRange(source.Options);
            }

            var process = new DesignerProcess();
            result.Process = process;

            var phase = new Phase();
            process.Phases.Add(phase);

            if (source.Steps.Count > 0)
            {
                phase.Steps.AddRange(source.Steps);
            }

            foreach (var property in source.Properties)
            {
                result.Properties.Add(property.Key, property.Value);
            }

            if (source.RetentionRules.Count > 0)
            {
                result.RetentionRules.AddRange(source.RetentionRules);
            }

            if (source.Tags.Count > 0)
            {
                result.Tags.AddRange(source.Tags);
            }

            if (source.Triggers.Count > 0)
            {
                result.Triggers.AddRange(source.Triggers);
            }

            foreach (var variablePair in source.Variables)
            {
                result.Variables.Add(variablePair.Key, variablePair.Value);
            }

            return result;
        }
    }
}
