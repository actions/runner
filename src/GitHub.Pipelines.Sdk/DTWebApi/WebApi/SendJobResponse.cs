using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class SendJobResponse
    {
        [JsonConstructor]
        public SendJobResponse()
        {
        }
        
        [DataMember]
        public JobEventsConfig Events
        {
            get
            {
                if (m_jobEventsConfig == null)
                {
                    m_jobEventsConfig = new JobEventsConfig();
                }

                return m_jobEventsConfig;
            }

            private set
            {
                m_jobEventsConfig = value;
            }
        }

        [DataMember]
        public IDictionary<String, String> Variables
        {
            get
            {
                if (m_variables == null)
                {
                    m_variables = new Dictionary<String, String>();
                }

                return m_variables;
            }
        }

        public SendJobResponse(IDictionary<String, String> variables, JobEventsConfig eventsConfig)
        {
            m_variables = variables;
            m_jobEventsConfig = eventsConfig;
        }

        private IDictionary<String, String> m_variables;

        private JobEventsConfig m_jobEventsConfig;
    }
}
