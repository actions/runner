using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Represents tasks in the task group.
    /// </summary>
    [DataContract]
    public class TaskGroupStep
    {
        public TaskGroupStep()
        {
        }

        private TaskGroupStep(TaskGroupStep taskGroupStep)
        {
            this.DisplayName = taskGroupStep.DisplayName;
            this.AlwaysRun = taskGroupStep.AlwaysRun;
            this.ContinueOnError = taskGroupStep.ContinueOnError;
            this.Enabled = taskGroupStep.Enabled;
            this.TimeoutInMinutes = taskGroupStep.TimeoutInMinutes;
            this.Inputs = new Dictionary<String, String>(taskGroupStep.Inputs);

            if (taskGroupStep.m_environment != null)
            {
                foreach (var property in taskGroupStep.m_environment)
                {
                    this.Environment[property.Key] = property.Value;
                }
            }

            this.Task = taskGroupStep.Task.Clone();
        }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        [DataMember]
        public String DisplayName
        {
            get
            {
                if (this.m_displayName == null)
                {
                    this.m_displayName = String.Empty;
                }

                return this.m_displayName;
            }
            set
            {
                this.m_displayName = value;
            }
        }

        /// <summary>
        /// Gets or sets as 'true' to run the task always, 'false' otherwise.
        /// </summary>
        [DataMember]
        public bool AlwaysRun { get; set; }

        /// <summary>
        /// Gets or sets as 'true' to continue on error, 'false' otherwise. 
        /// </summary>
        [DataMember]
        public bool ContinueOnError { get; set; }

        /// <summary>
        /// Gets or sets condition for the task.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Condition { get; set; }

        /// <summary>
        /// Gets or sets as task is enabled or not.
        /// </summary>
        [DataMember]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the maximum time, in minutes, that a task is allowed to execute on agent before being cancelled by server. A zero value indicates an infinite timeout.
        /// </summary>
        [DataMember]
        public int TimeoutInMinutes { get; set; }

        /// <summary>
        /// Gets or sets dictionary of inputs.
        /// </summary>
        [DataMember]
        public IDictionary<String, String> Inputs { get; set; }

        public IDictionary<string, string> Environment
        {
            get
            {
                if (m_environment == null)
                {
                    m_environment = new Dictionary<string, string>(StringComparer.Ordinal);
                }
                return m_environment;
            }
        }

        /// <summary>
        /// Gets dictionary of environment variables.
        /// </summary>
        [DataMember(Name = "Environment", EmitDefaultValue = false)]
        private Dictionary<string, string> m_environment;

        /// <summary>
        /// Gets or sets the reference of the task.
        /// </summary>
        [DataMember]
        public TaskDefinitionReference Task { get; set; }

        public static bool EqualsAndOldTaskInputsAreSubsetOfNewTaskInputs(
            TaskGroupStep oldTaskGroupStep,
            TaskGroupStep newTaskGroupStep)
        {
            if (!oldTaskGroupStep.DisplayName.Equals(newTaskGroupStep.DisplayName) 
                || oldTaskGroupStep.AlwaysRun != newTaskGroupStep.AlwaysRun
                || oldTaskGroupStep.Enabled != newTaskGroupStep.Enabled 
                || oldTaskGroupStep.ContinueOnError != newTaskGroupStep.ContinueOnError
                || !oldTaskGroupStep.Task.Equals(newTaskGroupStep.Task))
            {
                return false;
            }

            if (!(oldTaskGroupStep.Inputs != null && newTaskGroupStep.Inputs != null
                && oldTaskGroupStep.Inputs.Keys.All(key => newTaskGroupStep.Inputs.ContainsKey(key)
                && newTaskGroupStep.Inputs[key].Equals(oldTaskGroupStep.Inputs[key]))))
            {
                return false;
            }

            if (!(oldTaskGroupStep.Environment != null
                && oldTaskGroupStep.Environment.Keys.All(key => newTaskGroupStep.Environment.ContainsKey(key)
                && newTaskGroupStep.Environment[key].Equals(oldTaskGroupStep.Environment[key]))))
            {
                return false;
            }

            return true;
        }

        internal TaskGroupStep Clone()
        {
            return new TaskGroupStep(this);
        }

        private String m_displayName;
    }
}
