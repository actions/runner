using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi.Events
{
    [DataContract]
    [ServiceEventObject]
    [Obsolete("No longer used")]
    public class BuildPollingSummaryEvent
    {
        public BuildPollingSummaryEvent(Dictionary<String, String> ciData)
        {
            m_ciData = ciData;
        }

        public Dictionary<String, String> CIData
        {
            get { return m_ciData; }
        }

        private Dictionary<String, String> m_ciData;
    }
}
