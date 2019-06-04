using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class ResourceUsage
    {
        [DataMember]
        public ResourceLimit ResourceLimit
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32? UsedCount
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32? UsedMinutes
        {
            get;
            set;
        }

        public IList<TaskAgentJobRequest> RunningRequests
        {
            get
            {
                if (m_runningRequests == null)
                {
                    m_runningRequests = new List<TaskAgentJobRequest>();
                }

                return m_runningRequests;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_runningRequests?.Count == 0)
            {
                m_runningRequests = null;
            }
        }

        [DataMember(Name = "RunningRequests", EmitDefaultValue = false)]
        private IList<TaskAgentJobRequest> m_runningRequests;
    }
}
