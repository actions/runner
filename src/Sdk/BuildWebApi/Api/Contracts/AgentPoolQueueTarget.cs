using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GitHub.Services.WebApi;

namespace GitHub.Build.WebApi
{
    /// <summary>
    /// Describes how a phase should run against an agent queue.
    /// </summary>
    [DataContract]
    public class AgentPoolQueueTarget : PhaseTarget
    {
        public AgentPoolQueueTarget()
            : this(null)
        {
        }

        internal AgentPoolQueueTarget(
            ISecuredObject securedObject)
            : base(PhaseTargetType.Agent, securedObject)
        {
        }

        /// <summary>
        /// The queue.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public AgentPoolQueue Queue
        {
            get;
            set;
        }

        /// <summary>
        /// Agent specification of the target.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public AgentSpecification AgentSpecification
        {
            get;
            set;
        }

        /// <summary>
        /// The list of demands required for the queue.
        /// </summary>
        public List<Demand> Demands
        {
            get
            {
                if (m_demands == null)
                {
                    m_demands = new List<Demand>();
                }

                return m_demands;
            }
            set
            {
                m_demands = new List<Demand>(value);
            }
        }

        /// <summary>
        /// The execution options.
        /// </summary>
        [DataMember]
        public AgentTargetExecutionOptions ExecutionOptions
        {
            get;
            set;
        }

        /// <summary>
        /// Enables scripts and other processes launched while executing phase to access the OAuth token
        /// </summary>
        [DataMember]
        public Boolean AllowScriptsAuthAccessOption
        {
            get;
            set;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_serializedDemands, ref m_demands, true);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            SerializationHelper.Copy(ref m_demands, ref m_serializedDemands);
        }

        [DataMember(Name = "Demands", EmitDefaultValue = false)]
        private List<Demand> m_serializedDemands;

        private List<Demand> m_demands;
    }
}
