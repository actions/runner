using System;
using System.Runtime.Serialization;

namespace GitHub.Services.BlobStore.WebApi.Contracts
{
    /// <summary>
    /// Information about the Application Insights Instance.
    /// </summary>
    [DataContract]
    public class ApplicationInsightsInfo
    {
        public ApplicationInsightsInfo(string instrumentationkey)
        {
            InstrumentationKey = instrumentationkey;
        }

        /// <summary>
        /// Instrumentation Key.
        /// </summary>
        [DataMember(Name = "instrumentationkey")]
        public string InstrumentationKey { get; set; }
    }
}
