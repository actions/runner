using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DataContract]
    public class ResourceLimit
    {
        internal ResourceLimit(
            Guid hostId,
            String parallelismTag,
            Boolean isHosted)
        {
            HostId = hostId;
            ParallelismTag = parallelismTag;
            IsHosted = isHosted;
        }

        [DataMember]
        public Guid HostId
        {
            get;
            set;
        }

        [DataMember]
        public String ParallelismTag
        {
            get;
            set;
        }

        [DataMember]
        public Boolean IsHosted
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32? TotalCount
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Int32? TotalMinutes
        {
            get;
            set;
        }

        [DataMember]
        public Boolean IsPremium
        {
            get;
            set;
        }

        [DataMember(EmitDefaultValue = false)]
        public Boolean FailedToReachAllProviders
        {
            get;
            set;
        }

        public IDictionary<String, String> Data
        {
            get
            {
                if (m_resourceLimitsData == null)
                {
                    m_resourceLimitsData = new Dictionary<String, String>();
                }

                return m_resourceLimitsData;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_resourceLimitsData?.Count == 0)
            {
                m_resourceLimitsData = null;
            }
        }

        [DataMember(Name = "ResourceLimitsData", EmitDefaultValue = false)]
        private IDictionary<String, String> m_resourceLimitsData;
    }
}
