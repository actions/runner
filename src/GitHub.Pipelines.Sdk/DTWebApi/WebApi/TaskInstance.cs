using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    [DataContract]
    public sealed class TaskInstance : TaskReference
    {
        public TaskInstance()
        {
            // Enabled defaults to true
            this.Enabled = true;
        }

        private TaskInstance(TaskInstance taskToBeCloned)
            : base(taskToBeCloned)
        {
            this.InstanceId = taskToBeCloned.InstanceId;
            this.DisplayName = taskToBeCloned.DisplayName;
            this.Enabled = taskToBeCloned.Enabled;
            this.Condition = taskToBeCloned.Condition;
            this.ContinueOnError = taskToBeCloned.ContinueOnError;
            this.AlwaysRun = taskToBeCloned.AlwaysRun;
            this.TimeoutInMinutes = taskToBeCloned.TimeoutInMinutes;
            this.RefName = taskToBeCloned.RefName;

            if (taskToBeCloned.m_environment != null)
            {
                m_environment = new Dictionary<String, String>(taskToBeCloned.m_environment, StringComparer.Ordinal);
            }
        }

        [DataMember]
        public Guid InstanceId
        {
            get;
            set;
        }

        [DataMember]
        public String DisplayName
        {
            get;
            set;
        }

        [DataMember]
        public Boolean Enabled
        {
            get;
            set;
        }

        [DataMember]
        public String Condition
        {
            get;
            set;
        }

        [DataMember]
        public Boolean ContinueOnError
        {
            get;
            set;
        }

        [DataMember]
        public Boolean AlwaysRun
        {
            get;
            set;
        }

        [DataMember]
        public int TimeoutInMinutes
        {
            get;
            set;
        }

        [DataMember]
        public String RefName
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
                    m_environment = new Dictionary<String, String>(StringComparer.Ordinal);
                }
                return m_environment;
            }
        }

        public override TaskReference Clone()
        {
            return new TaskInstance(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedEnvironment, ref m_environment, StringComparer.Ordinal, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_environment, ref m_serializedEnvironment);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedEnvironment = null;
        }

        private IDictionary<String, String> m_environment;

        [DataMember(EmitDefaultValue = false, Name = "Environment")]
        private IDictionary<String, String> m_serializedEnvironment;
    }
}
