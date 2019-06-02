using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    [DebuggerDisplay("Id: {Id}, Name: {Name}, Version: {Version}")]
    public class TaskDefinition
    {
        public TaskDefinition()
        {
            this.DefinitionType = TaskDefinitionType.Task;
        }

        protected TaskDefinition(TaskDefinition taskDefinitionToClone)
        {
            if (taskDefinitionToClone.AgentExecution != null)
            {
                this.AgentExecution = taskDefinitionToClone.AgentExecution.Clone();
            }

            if (taskDefinitionToClone.PreJobExecution != null)
            {
                this.m_preJobExecution = new Dictionary<String, JObject>(taskDefinitionToClone.m_preJobExecution);
            }

            if (taskDefinitionToClone.Execution != null)
            {
                this.m_execution = new Dictionary<String, JObject>(taskDefinitionToClone.m_execution);
            }

            if (taskDefinitionToClone.PostJobExecution != null)
            {
                this.m_postJobExecution = new Dictionary<String, JObject>(taskDefinitionToClone.m_postJobExecution);
            }

            this.Author = taskDefinitionToClone.Author;
            this.Category = taskDefinitionToClone.Category;
            this.HelpMarkDown = taskDefinitionToClone.HelpMarkDown;
            this.HelpUrl = taskDefinitionToClone.HelpUrl;
            this.ContentsUploaded = taskDefinitionToClone.ContentsUploaded;

            if (taskDefinitionToClone.m_visibilities != null)
            {
                this.m_visibilities = new List<String>(taskDefinitionToClone.m_visibilities);
            }

            if (taskDefinitionToClone.m_runsOn != null)
            {
                this.m_runsOn = new List<String>(taskDefinitionToClone.m_runsOn);
            }

            if (this.m_runsOn == null)
            {
                this.m_runsOn = new List<String>(TaskRunsOnConstants.DefaultValue);
            }

            if (taskDefinitionToClone.m_demands != null)
            {
                this.m_demands = new List<Demand>(taskDefinitionToClone.m_demands.Select(x => x.Clone()));
            }

            this.Description = taskDefinitionToClone.Description;
            this.FriendlyName = taskDefinitionToClone.FriendlyName;
            this.HostType = taskDefinitionToClone.HostType;
            this.IconUrl = taskDefinitionToClone.IconUrl;
            this.Id = taskDefinitionToClone.Id;

            if (taskDefinitionToClone.m_inputs != null)
            {
                this.m_inputs = new List<TaskInputDefinition>(taskDefinitionToClone.m_inputs.Select(x => x.Clone()));
            }

            if (taskDefinitionToClone.m_satisfies != null)
            {
                this.m_satisfies = new List<String>(taskDefinitionToClone.m_satisfies);
            }

            if (taskDefinitionToClone.m_sourceDefinitions != null)
            {
                this.m_sourceDefinitions = new List<TaskSourceDefinition>(taskDefinitionToClone.m_sourceDefinitions.Select(x => x.Clone()));
            }

            if (taskDefinitionToClone.m_dataSourceBindings != null)
            {
                this.m_dataSourceBindings = new List<DataSourceBinding>(taskDefinitionToClone.m_dataSourceBindings.Select(x => x.Clone()));
            }

            if (taskDefinitionToClone.m_groups != null)
            {
                this.m_groups = new List<TaskGroupDefinition>(taskDefinitionToClone.m_groups.Select(x => x.Clone()));
            }

            if (taskDefinitionToClone.m_outputVariables != null)
            {
                this.m_outputVariables = new List<TaskOutputVariable>(taskDefinitionToClone.m_outputVariables.Select(x => x.Clone()));
            }

            this.InstanceNameFormat = taskDefinitionToClone.InstanceNameFormat;
            this.MinimumAgentVersion = taskDefinitionToClone.MinimumAgentVersion;
            this.Name = taskDefinitionToClone.Name;
            this.Ecosystem = taskDefinitionToClone.Ecosystem;
            this.PackageLocation = taskDefinitionToClone.PackageLocation;
            this.PackageType = taskDefinitionToClone.PackageType;
            this.ServerOwned = taskDefinitionToClone.ServerOwned;
            this.SourceLocation = taskDefinitionToClone.SourceLocation;
            this.Version = taskDefinitionToClone.Version.Clone();
            this.ContributionIdentifier = taskDefinitionToClone.ContributionIdentifier;
            this.ContributionVersion = taskDefinitionToClone.ContributionVersion;
            this.Deprecated = taskDefinitionToClone.Deprecated;
            this.Disabled = taskDefinitionToClone.Disabled;
            this.DefinitionType = taskDefinitionToClone.DefinitionType;
            this.ShowEnvironmentVariables = taskDefinitionToClone.ShowEnvironmentVariables;
            this.Preview = taskDefinitionToClone.Preview;
            this.ReleaseNotes = taskDefinitionToClone.ReleaseNotes;

            if (this.DefinitionType == null)
            {
                this.DefinitionType = TaskDefinitionType.Task;
            }
        }

        //
        // Members to identify this task
        //
        [DataMember(EmitDefaultValue = false)]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskVersion Version
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Ecosystem
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean ServerOwned
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean ContentsUploaded
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String IconUrl
        {
            get;
            set;
        }

        //
        // Location Information for acquisition
        //
        [DataMember(EmitDefaultValue = false)]
        public String HostType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String PackageType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String PackageLocation
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String SourceLocation
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String MinimumAgentVersion
        {
            get;
            set;
        }

        //
        // Helpful Metadata for discovery and designer
        //
        [DataMember(EmitDefaultValue = false)]
        public String FriendlyName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Description
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String Category
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String HelpMarkDown
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String HelpUrl
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ReleaseNotes
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean Preview
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean Deprecated
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ContributionIdentifier
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String ContributionVersion
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean Disabled
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String DefinitionType
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean ShowEnvironmentVariables
        {
            get;
            set;
        }

        public IList<String> Visibility
        {
            get
            {
                if (m_visibilities == null)
                {
                    m_visibilities = new List<String>();
                }
                return m_visibilities;
            }
        }

        public IList<String> RunsOn
        {
            get
            {
                if (m_runsOn == null)
                {
                    m_runsOn = new List<String>(TaskRunsOnConstants.DefaultValue);
                }

                return m_runsOn;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public String Author { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public IList<Demand> Demands
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

        [DataMember(EmitDefaultValue = false)]
        public IList<TaskGroupDefinition> Groups
        {
            get
            {
                if (m_groups == null)
                {
                    m_groups = new List<TaskGroupDefinition>();
                }
                return m_groups;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<TaskInputDefinition> Inputs
        {
            get
            {
                if (m_inputs == null)
                {
                    m_inputs = new List<TaskInputDefinition>();
                }
                return m_inputs;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<String> Satisfies
        {
            get
            {
                if (m_satisfies == null)
                {
                    m_satisfies = new List<String>();
                }
                return m_satisfies;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<TaskSourceDefinition> SourceDefinitions
        {
            get
            {
                if (m_sourceDefinitions == null)
                {
                    m_sourceDefinitions = new List<TaskSourceDefinition>();
                }
                return m_sourceDefinitions;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IList<DataSourceBinding> DataSourceBindings
        {
            get
            {
                if (m_dataSourceBindings == null)
                {
                    m_dataSourceBindings = new List<DataSourceBinding>();
                }
                return m_dataSourceBindings;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public String InstanceNameFormat
        {
            get;
            set;
        }

        //
        // Execution members
        //
        [DataMember(EmitDefaultValue = false)]
        public IDictionary<String, JObject> PreJobExecution
        {
            get
            {
                if (m_preJobExecution == null)
                {
                    m_preJobExecution = new Dictionary<String, JObject>();
                }
                return m_preJobExecution;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IDictionary<String, JObject> Execution
        {
            get
            {
                if (m_execution == null)
                {
                    m_execution = new Dictionary<String, JObject>();
                }
                return m_execution;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public IDictionary<String, JObject> PostJobExecution
        {
            get
            {
                if (m_postJobExecution == null)
                {
                    m_postJobExecution = new Dictionary<String, JObject>();
                }
                return m_postJobExecution;
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public TaskExecution AgentExecution
        {
            get;
            set;
        }

        public IList<TaskOutputVariable> OutputVariables
        {
            get
            {
                if (m_outputVariables == null)
                {
                    m_outputVariables = new List<TaskOutputVariable>();
                }
                return m_outputVariables;
            }
        }

        internal TaskDefinition Clone()
        {
            return new TaskDefinition(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedVisibilities, ref m_visibilities, true);
            SerializationHelper.Copy(ref m_serializedRunsOn, ref m_runsOn, true);
            RenameLegacyRunsOnValues(m_runsOn);
            SerializationHelper.Copy(ref m_serializedOutputVariables, ref m_outputVariables, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_visibilities, ref m_serializedVisibilities);
            RenameLegacyRunsOnValues(m_runsOn);
            SerializationHelper.Copy(ref m_runsOn, ref m_serializedRunsOn);
            SerializationHelper.Copy(ref m_outputVariables, ref m_serializedOutputVariables);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedVisibilities = null;
            m_serializedRunsOn = null;
            m_serializedOutputVariables = null;
        }

        private static void RenameLegacyRunsOnValues(IList<string> runsOn)
        {
            for (int i = 0; i < runsOn?.Count(); i++)
            {
                if (runsOn[i].Equals(TaskRunsOnConstants.RunsOnMachineGroup, StringComparison.OrdinalIgnoreCase))
                {
                    runsOn[i] = TaskRunsOnConstants.RunsOnDeploymentGroup;
                }
            }
        }

        //
        // Private
        //
        [DataMember(Name = "Visibility", EmitDefaultValue = false)]
        private List<String> m_serializedVisibilities;

        [DataMember(Name = "RunsOn", EmitDefaultValue = false)]
        private List<String> m_serializedRunsOn;

        [DataMember(Name = "OutputVariables", EmitDefaultValue = false)]
        private List<TaskOutputVariable> m_serializedOutputVariables;

        private Dictionary<String, JObject> m_preJobExecution;
        private Dictionary<String, JObject> m_execution;
        private Dictionary<String, JObject> m_postJobExecution;
        private List<Demand> m_demands;
        private List<TaskInputDefinition> m_inputs;
        private List<String> m_satisfies;
        private List<TaskSourceDefinition> m_sourceDefinitions;
        private List<DataSourceBinding> m_dataSourceBindings;
        private List<TaskGroupDefinition> m_groups;
        private List<TaskOutputVariable> m_outputVariables;
        private List<String> m_visibilities;
        private List<String> m_runsOn;
    }
}
