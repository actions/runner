using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.WebApi
{
    [ClientIgnore]
    [DataContract]
    public class TaskEventConfig
    {
        [JsonConstructor]
        public TaskEventConfig()
        {
        }

        public TaskEventConfig(String timeout, Boolean enabled = true)
        {
            m_timeout = timeout;
            m_enabled = new JValue(enabled);
        }

        public String Timeout
        {
            get
            {
                if (m_timeout == null)
                {
                    m_timeout = TimeSpan.Zero.ToString();
                }

                return m_timeout;
            }
        }
        
        internal JValue Enabled
        {
            get
            {
                if (m_enabled == null)
                {
                    m_enabled = new JValue(true);
                }

                return m_enabled;
            }

            set
            {
                m_enabled = value;
            }
        }

        [DataMember(Name = "Enabled")]
        private JValue m_enabled;

        [DataMember(Name = "Timeout")]
        private String m_timeout;
    }
}
