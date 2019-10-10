using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// A reference to a task definition.
    /// </summary>
    [DataContract]
    public class TaskDefinitionReference : BaseSecuredObject
    {
        public TaskDefinitionReference()
        {
        }

        public TaskDefinitionReference(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        /// <summary>
        /// The ID of the task.
        /// </summary>
        [DataMember(IsRequired = true)]
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>
        /// The version of the task.
        /// </summary>
        [DataMember(IsRequired = true)]
        public String VersionSpec
        {
            get;
            set;
        }

        /// <summary>
        /// The type of task (task or task group).
        /// </summary>
        [DataMember(IsRequired = false)]
        public String DefinitionType
        {
            get;
            set;
        }

        /// <summary>
        /// A clone of this reference.
        /// </summary>
        /// <returns></returns>
        public TaskDefinitionReference Clone()
        {
            return (TaskDefinitionReference)this.MemberwiseClone();
        }
    }

    /// <summary>
    /// Represents a step in a build phase.
    /// </summary>
    [DataContract]
    public class BuildDefinitionStep : BaseSecuredObject
    {
        public BuildDefinitionStep()
        {
        }

        internal BuildDefinitionStep(
            ISecuredObject securedObject)
            : base(securedObject)
        {
        }

        private BuildDefinitionStep(BuildDefinitionStep toClone)
        {
            ArgumentUtility.CheckForNull(toClone, nameof(toClone));

            this.Enabled = toClone.Enabled;
            this.ContinueOnError = toClone.ContinueOnError;
            this.AlwaysRun = toClone.AlwaysRun;
            this.DisplayName = toClone.DisplayName;
            this.TimeoutInMinutes = toClone.TimeoutInMinutes;
            this.Condition = toClone.Condition;
            this.RefName = toClone.RefName;

            // Cloning the reference type variables since memberwiseclone does a shallow copy
            if (toClone.TaskDefinition != null)
            {
                this.TaskDefinition = toClone.TaskDefinition.Clone();
            }

            if (toClone.m_inputs != null)
            {
                foreach (var property in toClone.m_inputs)
                {
                    this.Inputs.Add(property.Key, property.Value);
                }
            }

            if (toClone.m_environment != null)
            {
                foreach (var property in toClone.m_environment)
                {
                    this.Environment.Add(property.Key, property.Value);
                }
            }
        }

        /// <summary>
        /// The task associated with this step.
        /// </summary>
        [DataMember(IsRequired = true, Order = 1, Name = "Task")]
        public TaskDefinitionReference TaskDefinition
        {
            get;
            set;
        }

        /// <summary>
        /// The inputs used by this step.
        /// </summary>
        public IDictionary<String, String> Inputs
        {
            get
            {
                if (m_inputs == null)
                {
                    m_inputs = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_inputs;
            }
            set
            {
                m_inputs = new Dictionary<String, String>(value, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Indicates whether the step is enabled.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the phase should continue even if this step fails.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean ContinueOnError
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether this step should run even if a previous step fails.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Boolean AlwaysRun
        {
            get;
            set;
        }

        /// <summary>
        /// The display name for this step.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String DisplayName
        {
            get;
            set;
        }

        /// <summary>
        /// The time, in minutes, that this step is allowed to run.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public Int32 TimeoutInMinutes
        {
            get;
            set;
        }

        /// <summary>
        /// A condition that determines whether this step should run.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public String Condition
        {
            get;
            set;
        }

        /// <summary>
        /// The reference name for this step.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String RefName
        {
            get;
            set;
        }

        /// <summary>
        /// The run-time environment for this step.
        /// </summary>
        public IDictionary<String, String> Environment
        {
            get
            {
                if (m_environment == null)
                {
                    m_environment = new Dictionary<String, String>(StringComparer.Ordinal);
                }

                return m_environment;
            }
            set
            {
                m_environment = new Dictionary<String, String>(value, StringComparer.Ordinal);
            }
        }

        /// <summary>
        /// A clone of this step.
        /// </summary>
        /// <returns></returns>
        public BuildDefinitionStep Clone()
        {
            return new BuildDefinitionStep(this);
        }

        [DataMember(Name = "Environment", EmitDefaultValue = false)]
        private Dictionary<String, String> m_environment;

        [DataMember(Name = "Inputs", EmitDefaultValue = false, Order = 2)]
        private Dictionary<String, String> m_inputs;
    }
}
