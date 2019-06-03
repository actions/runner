using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Represents a symmetric key used for message-level encryption for communication sent to an agent.
    /// </summary>
    [DataContract]
    public sealed class TaskAgentSessionKey
    {
        /// <summary>
        /// Gets or sets a value indicating whether or not the key value is encrypted. If this value is true, the 
        /// <see cref="Value"/> property should be decrypted using the <c>RSA</c> key exchanged with the server during
        /// registration.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Boolean Encrypted
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the symmetric key value.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Byte[] Value
        {
            get;
            set;
        }
    }
}
