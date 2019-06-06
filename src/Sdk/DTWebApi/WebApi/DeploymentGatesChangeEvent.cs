using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [ServiceEventObject]
    public class DeploymentGatesChangeEvent
    {
        public DeploymentGatesChangeEvent(IList<string> ignoredGates)
        {
            m_ignoredGates = ignoredGates;
        }

        public IList<string> IgnoredGates
        {
            get
            {
                if (m_ignoredGates == null)
                {
                    m_ignoredGates = new List<string>();
                }

                return m_ignoredGates;
            }
        }

        [DataMember(Name = "GateNames")]
        private IList<string> m_ignoredGates;
    }
}
