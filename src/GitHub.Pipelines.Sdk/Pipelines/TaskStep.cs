using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Newtonsoft.Json;

namespace Microsoft.TeamFoundation.DistributedTask.Pipelines
{
    [DataContract]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class TaskStep : JobStep
    {
        [JsonConstructor]
        public TaskStep()
        {
        }

        internal TaskStep(TaskInstance legacyTaskInstance)
        {
            this.ContinueOnError = legacyTaskInstance.ContinueOnError;
            this.DisplayName = legacyTaskInstance.DisplayName;
            this.Enabled = legacyTaskInstance.Enabled;
            this.Id = legacyTaskInstance.InstanceId;
            this.Name = legacyTaskInstance.RefName;
            this.TimeoutInMinutes = legacyTaskInstance.TimeoutInMinutes;
            this.Reference = new TaskStepDefinitionReference()
            {
                Id = legacyTaskInstance.Id,
                Name = legacyTaskInstance.Name,
                Version = legacyTaskInstance.Version
            };

            if (!String.IsNullOrEmpty(legacyTaskInstance.Condition))
            {
                this.Condition = legacyTaskInstance.Condition;
            }
            else if (legacyTaskInstance.AlwaysRun)
            {
                this.Condition = "succeededOrFailed()";
            }
            else
            {
                this.Condition = "succeeded()";
            }

            foreach (var input in legacyTaskInstance.Inputs)
            {
                this.Inputs[input.Key] = input.Value;
            }

            foreach (var env in legacyTaskInstance.Environment)
            {
                this.Environment[env.Key] = env.Value;
            }
        }

        private TaskStep(TaskStep taskToClone)
            : base(taskToClone)
        {
            this.Reference = taskToClone.Reference?.Clone();

            if (taskToClone.m_environment?.Count > 0)
            {
                m_environment = new Dictionary<String, String>(taskToClone.m_environment, StringComparer.OrdinalIgnoreCase);
            }

            if (taskToClone.m_inputs?.Count > 0)
            {
                m_inputs = new Dictionary<String, String>(taskToClone.m_inputs, StringComparer.OrdinalIgnoreCase);
            }
        }

        public override StepType Type => StepType.Task;

        [DataMember]
        public TaskStepDefinitionReference Reference
        {
            get;
            set;
        }

        public IDictionary<String, String> Environment
        {
            get
            {
                if (m_environment == null)
                {
                    m_environment = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_environment;
            }
        }

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
        }

        public override Step Clone()
        {
            return new TaskStep(this);
        }

        internal TaskInstance ToLegacyTaskInstance()
        {
            TaskInstance task = new TaskInstance()
            {
                AlwaysRun = String.Equals(this.Condition ?? String.Empty, "succeededOrFailed()", StringComparison.Ordinal),
                Condition = this.Condition,
                ContinueOnError = this.ContinueOnError,
                DisplayName = this.DisplayName,
                Enabled = this.Enabled,
                InstanceId = this.Id,
                RefName = this.Name,
                TimeoutInMinutes = this.TimeoutInMinutes,
                Id = this.Reference.Id,
                Name = this.Reference.Name,
                Version = this.Reference.Version,
            };

            foreach (var env in this.Environment)
            {
                task.Environment[env.Key] = env.Value;
            }

            foreach (var input in this.Inputs)
            {
                task.Inputs[input.Key] = input.Value;
            }

            return task;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_environment?.Count == 0)
            {
                m_environment = null;
            }

            if (m_inputs?.Count == 0)
            {
                m_inputs = null;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (m_environment != null)
            {
                m_environment = new Dictionary<String, String>(m_environment, StringComparer.OrdinalIgnoreCase);
            }

            if (m_inputs != null)
            {
                m_inputs = new Dictionary<String, String>(m_inputs, StringComparer.OrdinalIgnoreCase);
            }
        }

        [DataMember(Name = "Environment", EmitDefaultValue = false)]
        private IDictionary<String, String> m_environment;

        [DataMember(Name = "Inputs", EmitDefaultValue = false)]
        private IDictionary<String, String> m_inputs;
    }
}
