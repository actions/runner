using System;
using System.Runtime.Serialization;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Provides a contract for receiving messages from the task orchestrator.
    /// </summary>
    [DataContract]
    public sealed class TaskAgentMessage
    {
        /// <summary>
        /// Initializes an empty <c>TaskAgentMessage</c> instance.
        /// </summary>
        public TaskAgentMessage()
        {
        }

        /// <summary>
        /// Gets or sets the message identifier.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Int64 MessageId
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the message type, describing the data contract found in <c>TaskAgentMessage.Body</c>.
        /// </summary>
        [DataMember]
        public String MessageType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the intialization vector used to encrypt this message.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Byte[] IV
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the body of the message. If the <c>IV</c> property is provided the body will need to be
        /// decrypted using the <c>TaskAgentSession.EncryptionKey</c> value in addition to the <c>IV</c>.
        /// </summary>
        [DataMember]
        public String Body
        {
            get;
            set;
        }
    }
}
