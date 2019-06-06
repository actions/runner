using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskReference : ITaskDefinitionReference
    {
        public TaskReference()
        {
        }

        protected TaskReference(TaskReference taskToBeCloned)
        {
            this.Id = taskToBeCloned.Id;
            this.Name = taskToBeCloned.Name;
            this.Version = taskToBeCloned.Version;

            if (taskToBeCloned.m_inputs != null)
            {
                m_inputs = new Dictionary<String, String>(taskToBeCloned.m_inputs, StringComparer.OrdinalIgnoreCase);
            }
        }

        [DataMember]
        public Guid Id
        {
            get;
            set;
        }

        [DataMember]
        public String Name
        {
            get;
            set;
        }

        [DataMember]
        public String Version
        {
            get;
            set;
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

        public virtual TaskReference Clone()
        {
            return new TaskReference(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedInputs, ref m_inputs, StringComparer.OrdinalIgnoreCase, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_inputs, ref m_serializedInputs);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedInputs = null;
        }

        private IDictionary<String, String> m_inputs;

        [DataMember(EmitDefaultValue = false, Name = "Inputs")]
        private IDictionary<String, String> m_serializedInputs;
    }
}
