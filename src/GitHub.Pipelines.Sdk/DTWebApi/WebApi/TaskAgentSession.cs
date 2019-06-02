using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.TeamFoundation.DistributedTask.WebApi
{
    /// <summary>
    /// Represents a session for performing message exchanges from an agent.
    /// </summary>
    [DataContract]
    public class TaskAgentSession
    {
        public TaskAgentSession()
        {
        }

        /// <summary>
        /// Initializes a new <c>TaskAgentSession</c> instance with the specified owner name and agent.
        /// </summary>
        /// <param name="ownerName">The name of the owner for this session. This should typically be the agent machine</param>
        /// <param name="agent">The target agent for the session</param>
        public TaskAgentSession(
            String ownerName, 
            TaskAgentReference agent)
        {
            this.Agent = agent;
            this.OwnerName = ownerName;
        }

        /// <summary>
        /// Initializes a new <c>TaskAgentSession</c> isntance with the specified owner name, agent, and capabilities.
        /// </summary>
        /// <param name="ownerName">The name of the owner for this session. This should typically be the agent machine</param>
        /// <param name="agent">The target agent for the session</param>
        /// <param name="systemCapabilities">A collection of capabilities to publish on session creation</param>
        public TaskAgentSession(
            String ownerName,
            TaskAgentReference agent,
            IDictionary<String, String> systemCapabilities)
        {
            this.Agent = agent;
            this.OwnerName = ownerName;

            foreach (var capability in systemCapabilities)
            {
                if (capability.Value != null)
                {
                    this.SystemCapabilities.Add(capability.Key, capability.Value);
                }
            }
        }

        /// <summary>
        /// Gets the unique identifier for this session.
        /// </summary>
        [DataMember]
        public Guid SessionId
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the key used to encrypt message traffic for this session.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentSessionKey EncryptionKey
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the owner name of this session. Generally this will be the machine of origination.
        /// </summary>
        [DataMember]
        public String OwnerName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the agent which is the target of the session.
        /// </summary>
        [DataMember]
        public TaskAgentReference Agent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the collection of system capabilities used for this session.
        /// </summary>
        public IDictionary<String, String> SystemCapabilities
        {
            get
            {
                if (m_systemCapabilities == null)
                {
                    m_systemCapabilities = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                }
                return m_systemCapabilities;
            }
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            if (m_systemCapabilities?.Count == 0)
            {
                m_systemCapabilities = null;
            }
        }


        [DataMember(IsRequired = false, EmitDefaultValue = false, Name = "SystemCapabilities")]
        private IDictionary<String, String> m_systemCapabilities;
    }
}
