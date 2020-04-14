using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class AgentLabel
    {
        [JsonConstructor]
        public AgentLabel()
        {
        }

        public AgentLabel(string name)
        {
            this.Name = name;
            this.Type = LabelType.System;
        }

        public AgentLabel(string name, LabelType type)
        {
            this.Name = name;
            this.Type = type;
        }

        private AgentLabel(AgentLabel labelToBeCloned)
        {
            this.Id = labelToBeCloned.Id;
            this.Name = labelToBeCloned.Name;
            this.Type = labelToBeCloned.Type;
        }

        [DataMember]
        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public string Name
        {
            get;
            set;
        }

        [DataMember]
        public LabelType Type
        {
            get;
            set;
        }

        public AgentLabel Clone()
        {
            return new AgentLabel(this);
        }
    }
}
