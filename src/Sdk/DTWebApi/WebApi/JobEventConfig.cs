using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public class JobEventConfig
    {
        public JobEventConfig(String timeout)
        {
            m_timeout = timeout;
        }

        [DataMember(Name = "Timeout")]
        public String Timeout
        {
            get
            {
                if (m_timeout == null)
                {
                    m_timeout = String.Empty;
                }

                return m_timeout;
            }
        }

        private String m_timeout;
    }
}
