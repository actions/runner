using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    [JsonConverter(typeof(JobRequestMessageJsonConverter))]

    public abstract class JobRequestMessage
    {
        protected JobRequestMessage(string messageType)
        {
            this.MessageType = messageType;
        }

        protected JobRequestMessage(
            string messageType,
            TaskOrchestrationPlanReference plan,
            TimelineReference timeline,
            Guid jobId,
            String jobName,
            String jobRefName,
            JobEnvironment environment)
        {
            this.MessageType = messageType;
            this.Plan = plan;
            this.JobId = jobId;
            this.JobName = jobName;
            this.JobRefName = jobRefName;
            this.Timeline = timeline;
            this.Environment = environment;
        }

        [DataMember]
        public String MessageType
        {
            get;
            private set;
        }
      
        [DataMember]
        public TaskOrchestrationPlanReference Plan
        {
            get;
            private set;
        }

        [DataMember]
        public TimelineReference Timeline
        {
            get;
            private set;
        }

        [DataMember]
        public Guid JobId
        {
            get;
            private set;
        }

        [DataMember]
        public String JobName
        {
            get;
            private set;
        }

        [DataMember]
        public String JobRefName
        {
            get;
            private set;
        }

        [DataMember]
        public JobEnvironment Environment
        {
            get;
            private set;
        }
    }
}
