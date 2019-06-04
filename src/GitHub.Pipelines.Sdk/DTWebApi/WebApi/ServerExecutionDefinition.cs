using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class ServerExecutionDefinition
    {
        [JsonConstructor]
        internal ServerExecutionDefinition()
        {
        }

        protected ServerExecutionDefinition(string handlerName)
        {
            HandlerName = handlerName;
        }

        [DataMember]
        public String HandlerName
        {
            get;
        }
       
        public EventsConfig EventsConfig
        {
            get
            {
                if (m_EventsConfig == null)
                {
                    m_EventsConfig = new JobEventsConfig();
                }

                return m_EventsConfig;
            }

            set
            {
                m_EventsConfig = value;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "Events")]
        private EventsConfig m_EventsConfig;
    }
}
