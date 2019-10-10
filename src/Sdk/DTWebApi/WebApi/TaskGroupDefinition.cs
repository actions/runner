using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class TaskGroupDefinition
    {
        public TaskGroupDefinition()
        {
            IsExpanded = false;
        }

        private TaskGroupDefinition(TaskGroupDefinition inputDefinitionToClone)
        {
            this.IsExpanded = inputDefinitionToClone.IsExpanded;
            this.Name = inputDefinitionToClone.Name;
            this.DisplayName = inputDefinitionToClone.DisplayName;
            this.VisibleRule = inputDefinitionToClone.VisibleRule;
            
            if (inputDefinitionToClone.m_tags != null)
            {
                this.m_tags = new List<String>(inputDefinitionToClone.m_tags);
            }
        }

        [DataMember(EmitDefaultValue = false)]
        public String Name
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public String DisplayName
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = true)]
        public Boolean IsExpanded
        {
            get;
            set;
        }
        
        [DataMember(EmitDefaultValue = false)]
        public String VisibleRule
        {
            get;
            set;
        }

        public IList<String> Tags
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

        public TaskGroupDefinition Clone()
        {
            return new TaskGroupDefinition(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedTags, ref m_tags, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_tags, ref m_serializedTags);
        }
        
        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedTags = null;
        }
        
        [DataMember(Name = "Tags", EmitDefaultValue = false)]
        private List<String> m_serializedTags;
        
        private List<String> m_tags;
    }
}
