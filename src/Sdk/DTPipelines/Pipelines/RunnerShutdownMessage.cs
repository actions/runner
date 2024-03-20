using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.Pipelines
{
    [DataContract]
    public sealed class HostedRunnerShutdownMessage
    {
        public static readonly String MessageType = "RunnerShutdown";

        [JsonConstructor]
        internal HostedRunnerShutdownMessage()
        {
        }

        public HostedRunnerShutdownMessage(String reason)
        {
            this.Reason = reason;
        }

        [DataMember]
        public String Reason
        {
            get;
            private set;
        }

        public WebApi.TaskAgentMessage GetAgentMessage()
        {
            return new WebApi.TaskAgentMessage
            {
                Body = JsonUtility.ToString(this),
                MessageType = HostedRunnerShutdownMessage.MessageType,
            };
        }
    }
}
