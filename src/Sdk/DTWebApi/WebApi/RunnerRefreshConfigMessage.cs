using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class RunnerRefreshConfigMessage
    {
        public static readonly String MessageType = "RunnerRefreshConfig";

        [JsonConstructor]
        internal RunnerRefreshConfigMessage()
        {
        }

        public RunnerRefreshConfigMessage(
            string runnerQualifiedId,
            string configType,
            string serviceType,
            string configRefreshUrl)
        {
            this.RunnerQualifiedId = runnerQualifiedId;
            this.ConfigType = configType;
            this.ServiceType = serviceType;
            this.ConfigRefreshUrl = configRefreshUrl;
        }

        [DataMember(Name = "runnerQualifiedId")]
        public String RunnerQualifiedId
        {
            get;
            private set;
        }

        [DataMember(Name = "configType")]
        public String ConfigType
        {
            get;
            private set;
        }

        [DataMember(Name = "serviceType")]
        public String ServiceType
        {
            get;
            private set;
        }

        [DataMember(Name = "configRefreshURL")]
        public String ConfigRefreshUrl
        {
            get;
            private set;
        }
    }
}
