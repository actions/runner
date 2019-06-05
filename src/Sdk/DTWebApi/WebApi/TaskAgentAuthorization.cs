using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Provides data necessary for authorizing the agent using OAuth 2.0 authentication flows.
    /// </summary>
    [DataContract]
    public sealed class TaskAgentAuthorization
    {
        /// <summary>
        /// Initializes a new <c>TaskAgentAuthorization</c> instance with default values.
        /// </summary>
        public TaskAgentAuthorization()
        {
        }

        private TaskAgentAuthorization(TaskAgentAuthorization objectToBeCloned)
        {
            this.AuthorizationUrl = objectToBeCloned.AuthorizationUrl;
            this.ClientId = objectToBeCloned.ClientId;

            if (objectToBeCloned.PublicKey != null)
            {
                this.PublicKey = objectToBeCloned.PublicKey.Clone();
            }
        }

        /// <summary>
        /// Endpoint used to obtain access tokens from the configured token service.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Uri AuthorizationUrl
        {
            get;
            set;
        }

        /// <summary>
        /// Client identifier for this agent.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Guid ClientId
        {
            get;
            set;
        }

        /// <summary>
        /// Public key used to verify the identity of this agent.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TaskAgentPublicKey PublicKey
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a deep copy of the authorization data.
        /// </summary>
        /// <returns>A new <c>TaskAgentAuthorization</c> instance copied from the current instance</returns>
        public TaskAgentAuthorization Clone()
        {
            return new TaskAgentAuthorization(this);
        }
    }
}
