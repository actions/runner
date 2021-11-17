using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// A job request for an agent.
    /// </summary>
    [DataContract]
    public class TaskAgentJobRequest : ICloneable
    {
        public TaskAgentJobRequest()
        {
        }

        private TaskAgentJobRequest(TaskAgentJobRequest requestToBeCloned)
        {
            this.RequestId = requestToBeCloned.RequestId;
            this.QueueTime = requestToBeCloned.QueueTime;
            this.AssignTime = requestToBeCloned.AssignTime;
            this.ReceiveTime = requestToBeCloned.ReceiveTime;
            this.FinishTime = requestToBeCloned.FinishTime;
            this.Result = requestToBeCloned.Result;
            this.LockedUntil = requestToBeCloned.LockedUntil;
            this.ServiceOwner = requestToBeCloned.ServiceOwner;
            this.HostId = requestToBeCloned.HostId;
            this.ScopeId = requestToBeCloned.ScopeId;
            this.PlanType = requestToBeCloned.PlanType;
            this.PlanGroup = requestToBeCloned.PlanGroup;
            this.PlanId = requestToBeCloned.PlanId;
            this.QueueId = requestToBeCloned.QueueId;
            this.PoolId = requestToBeCloned.PoolId;
            this.JobId = requestToBeCloned.JobId;
            this.JobName = requestToBeCloned.JobName;
            this.LockToken = requestToBeCloned.LockToken;
            this.ExpectedDuration = requestToBeCloned.ExpectedDuration;
            this.OrchestrationId = requestToBeCloned.OrchestrationId;
            this.MatchesAllAgentsInPool = requestToBeCloned.MatchesAllAgentsInPool;

            if (requestToBeCloned.m_matchedAgents != null && requestToBeCloned.m_matchedAgents.Count > 0)
            {
                m_matchedAgents = requestToBeCloned.m_matchedAgents.Select(x => x.Clone()).ToList();
            }

            if (requestToBeCloned.ReservedAgent != null)
            {
                this.ReservedAgent = requestToBeCloned.ReservedAgent.Clone();
            }

            if (requestToBeCloned.m_requestAgentData?.Count > 0)
            {
                foreach (var pair in requestToBeCloned.m_requestAgentData)
                {
                    this.Data[pair.Key] = pair.Value;
                }
            }

            if (requestToBeCloned.AgentSpecification != null)
            {
                this.AgentSpecification = new JObject(requestToBeCloned.AgentSpecification);
            }

            if (requestToBeCloned.Labels != null)
            {
                this.Labels = new HashSet<string>(requestToBeCloned.Labels, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// ID of the request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 2)]
        public Int64 RequestId
        {
            get;
            internal set;
        }

        /// <summary>
        /// The date/time this request was queued.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 3, EmitDefaultValue = false)]
        public DateTime QueueTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// The date/time this request was assigned.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 4, EmitDefaultValue = false)]
        public DateTime? AssignTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// The date/time this request was receieved by an agent.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 5, EmitDefaultValue = false)]
        public DateTime? ReceiveTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// The date/time this request was finished.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 6, EmitDefaultValue = false)]
        public DateTime? FinishTime
        {
            get;
            internal set;
        }

        /// <summary>
        /// The result of this request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 8, EmitDefaultValue = false)]
        public TaskResult? Result
        {
            get;
            set;
        }

        /// <summary>
        /// The deadline for the agent to renew the lock.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 9, EmitDefaultValue = false)]
        public DateTime? LockedUntil
        {
            get;
            internal set;
        }

        /// <summary>
        /// The service which owns this request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 10, EmitDefaultValue = false)]
        public Guid ServiceOwner
        {
            get;
            set;
        }

        /// <summary>
        /// The host which triggered this request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 11, EmitDefaultValue = false)]
        public Guid HostId
        {
            get;
            set;
        }

        /// <summary>
        /// Scope of the pipeline; matches the project ID.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 12, EmitDefaultValue = false)]
        public Guid ScopeId
        {
            get;
            set;
        }

        /// <summary>
        /// Internal detail representing the type of orchestration plan.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 13, EmitDefaultValue = false)]
        public String PlanType
        {
            get;
            set;
        }

        /// <summary>
        /// Internal ID for the orchestration plan connected with this request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 14, EmitDefaultValue = false)]
        public Guid PlanId
        {
            get;
            set;
        }

        /// <summary>
        /// ID of the job resulting from this request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 15, EmitDefaultValue = false)]
        public Guid JobId
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the job resulting from this request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 21, EmitDefaultValue = false)]
        public String JobName
        {
            get;
            set;
        }

        /// <summary>
        /// The agent allocated for this request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 17, EmitDefaultValue = false)]
        public TaskAgentReference ReservedAgent
        {
            get;
            internal set;
        }

        public List<TaskAgentReference> MatchedAgents
        {
            get
            {
                if (m_matchedAgents == null)
                {
                    m_matchedAgents = new List<TaskAgentReference>();
                }
                return m_matchedAgents;
            }
        }

        /// <summary>
        /// The pipeline definition associated with this request
        /// </summary>
        /// <value></value>
        [DataMember(Order = 19, EmitDefaultValue = false)]
        public TaskOrchestrationOwner Definition
        {
            get;
            set;
        }

        /// <summary>
        /// The pipeline associated with this request
        /// </summary>
        /// <value></value>
        [DataMember(Order = 20, EmitDefaultValue = false)]
        public TaskOrchestrationOwner Owner
        {
            get;
            set;
        }

        /// <summary>
        /// Additional data about the request.
        /// </summary>
        /// <value></value>
        [DataMember(Order = 22, EmitDefaultValue = false)]
        public IDictionary<String, String> Data
        {
            get
            {
                if (m_requestAgentData == null)
                {
                    m_requestAgentData = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }

                return m_requestAgentData;
            }
        }

        [DataMember(Order = 23, EmitDefaultValue = false)]
        public String PlanGroup
        {
            get;
            set;
        }

        /// <summary>
        /// The ID of the pool this request targets
        /// </summary>
        /// <value></value>
        [DataMember(Order = 24, EmitDefaultValue = false)]
        internal Int32 PoolId
        {
            get;
            set;
        }

        /// <summary>
        /// The ID of the queue this request targets
        /// </summary>
        /// <value></value>
        [DataMember(Order = 25, EmitDefaultValue = false)]
        internal Int32? QueueId
        {
            get;
            set;
        }

        [DataMember(Order = 27, EmitDefaultValue = false)]
        public TimeSpan? ExpectedDuration
        {
            get;
            set;
        }

        [DataMember(Order = 28, EmitDefaultValue = false)]
        public JObject AgentSpecification
        {
            get;
            set;
        }

        [DataMember(Order = 29, EmitDefaultValue = false)]
        public String OrchestrationId
        {
            get;
            set;
        }

        [DataMember(Order = 30, EmitDefaultValue = false)]
        public Boolean MatchesAllAgentsInPool
        {
            get;
            set;
        }

        [DataMember(Order = 31, EmitDefaultValue = false)]
        public String StatusMessage
        {
            get;
            set;
        }

        [DataMember(Order = 32, EmitDefaultValue = false)]
        public bool UserDelayed
        {
            get;
            set;
        }

        [DataMember(Order = 33, EmitDefaultValue = false)]
        public ISet<string> Labels
        {
            get;
            set;
        }

        [IgnoreDataMember]
        internal Guid? LockToken
        {
            get;
            set;
        }

        Object ICloneable.Clone()
        {
            return this.Clone();
        }

        public TaskAgentJobRequest Clone()
        {
            return new TaskAgentJobRequest(this);
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedMatchedAgents, ref m_matchedAgents, true);
        }

        [OnSerialized]
        private void OnSerialized(StreamingContext context)
        {
            m_serializedMatchedAgents = null;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_matchedAgents, ref m_serializedMatchedAgents);
        }

        private List<TaskAgentReference> m_matchedAgents;

        private IDictionary<String, String> m_requestAgentData;

        [DataMember(Name = "MatchedAgents", Order = 18, EmitDefaultValue = false)]
        private List<TaskAgentReference> m_serializedMatchedAgents;
    }
}
