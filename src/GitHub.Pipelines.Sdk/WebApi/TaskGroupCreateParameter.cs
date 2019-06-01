using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public class TaskGroupCreateParameter
    {
        /// <summary>
        /// Sets name of the task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Name { get; set; }

        /// <summary>
        /// Sets friendly name of the task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String FriendlyName { get; set; }

        /// <summary>
        /// Sets author name of the task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Author { get; set; }

        /// <summary>
        /// Sets description of the task group. 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Description { get; set; }

        /// <summary>
        /// Sets parent task group Id. This is used while creating a draft task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid? ParentDefinitionId { get; set; }

        /// <summary>
        /// Sets url icon of the task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String IconUrl { get; set; }

        /// <summary>
        /// Sets display name of the task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String InstanceNameFormat { get; set; }

        /// <summary>
        /// Sets category of the task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Category { get; set; }

        /// <summary>
        /// Sets version of the task group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskVersion Version { get; set; }

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

        public IList<TaskGroupStep> Tasks
        {
            get
            {
                if (m_tasks == null)
                {
                    m_tasks = new List<TaskGroupStep>();
                }

                return m_tasks;
            }
        }

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

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedRunsOn, ref m_runsOn, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_runsOn, ref m_serializedRunsOn);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedRunsOn = null;
        }

        /// <summary>
        /// Sets RunsOn of the task group. Value can be 'Agent', 'Server' or 'DeploymentGroup'.
        /// </summary>
        [DataMember(Name = "RunsOn", EmitDefaultValue = false)]
        private List<String> m_serializedRunsOn;

        /// <summary>
        /// Sets tasks for the task group.
        /// </summary>
        [DataMember(Name = "Tasks", EmitDefaultValue = false)]
        private IList<TaskGroupStep> m_tasks;

        /// <summary>
        /// Sets input for the task group.
        /// </summary>
        [DataMember(Name = "Inputs", EmitDefaultValue = false)]
        private List<TaskInputDefinition> m_inputs;

        private List<String> m_runsOn;
    }
}
