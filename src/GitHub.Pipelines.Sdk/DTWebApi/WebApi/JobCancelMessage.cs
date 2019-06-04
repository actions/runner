using System;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class JobCancelMessage
    {
        public static readonly String MessageType = "JobCancellation";

        [JsonConstructor]
        internal JobCancelMessage()
        {
        }

        public JobCancelMessage(Guid jobId, TimeSpan timeout)
        {
            this.JobId = jobId;
            this.Timeout = timeout;
        }

        [DataMember]
        public Guid JobId
        {
            get;
            private set;
        }

        [DataMember]
        public TimeSpan Timeout
        {
            get;
            private set;
        }

        public TaskAgentMessage GetAgentMessage()
        {
            return new TaskAgentMessage
            {
                Body = JsonUtility.ToString(this),
                MessageType = JobCancelMessage.MessageType,
            };
        }
    }
}
