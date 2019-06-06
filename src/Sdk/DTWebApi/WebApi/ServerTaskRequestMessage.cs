using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]
    public sealed class ServerTaskRequestMessage : JobRequestMessage
    {
        internal ServerTaskRequestMessage() 
            : base(JobRequestMessageTypes.ServerTaskRequest)
        {
        }

        public ServerTaskRequestMessage(
            TaskOrchestrationPlanReference plan,
            TimelineReference timeline,
            Guid jobId,
            String jobName,
            String jobRefName,
            JobEnvironment environment,
            TaskInstance taskInstance,
            TaskDefinition taskDefinition) 
            : base(JobRequestMessageTypes.ServerJobRequest, plan, timeline, jobId, jobName, jobRefName, environment)
        {
            TaskDefinition = taskDefinition;
            TaskInstance = taskInstance;
        }

        [DataMember]
        public TaskDefinition TaskDefinition
        {
            get;
            private set;
        }

        [DataMember]
        public TaskInstance TaskInstance
        {
            get;
            private set;
        }
    }
}
