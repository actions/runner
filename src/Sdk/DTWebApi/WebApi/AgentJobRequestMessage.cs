using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;
using Newtonsoft.Json;

namespace GitHub.DistributedTask.WebApi
{
    [DataContract]

    public sealed class AgentJobRequestMessage : JobRequestMessage
    {
        [JsonConstructor]
        internal AgentJobRequestMessage() : base(JobRequestMessageTypes.AgentJobRequest)
        {
        }

        public AgentJobRequestMessage(
            TaskOrchestrationPlanReference plan,
            TimelineReference timeline,
            Guid jobId,
            String jobName,
            String jobRefName,
            JobEnvironment environment,
            IEnumerable<TaskInstance> tasks) 
            : base(JobRequestMessageTypes.AgentJobRequest, plan, timeline, jobId, jobName, jobRefName, environment)
        {
            m_tasks = new List<TaskInstance>(tasks);
        }

        [DataMember]
        public Int64 RequestId
        {
            get;
            internal set;
        }

        [DataMember]
        public Guid LockToken
        {
            get;
            internal set;
        }

        [DataMember]
        public DateTime LockedUntil
        {
            get;
            internal set;
        }

        public ReadOnlyCollection<TaskInstance> Tasks
        {
            get
            {
                if (m_tasks == null)
                {
                    m_tasks = new List<TaskInstance>();
                }
                return m_tasks.AsReadOnly();
            }
        }

        public TaskAgentMessage GetAgentMessage()
        {
            var body = JsonUtility.ToString(this);

            return new TaskAgentMessage
            {
                Body = body,
                MessageType = JobRequestMessageTypes.AgentJobRequest
            };
        }

        [DataMember(Name = "Tasks", EmitDefaultValue = false)]
        private List<TaskInstance> m_tasks;
    }
}
