using System;
using System.Runtime.Serialization;
using GitHub.Services.Common;

namespace GitHub.DistributedTask.WebApi
{
    /// <summary>
    /// Represents the public key portion of an RSA asymmetric key.
    /// </summary>
    [DataContract]
    public sealed class TaskAgentPublicKey
    {
        /// <summary>
        /// Initializes a new <c>TaskAgentPublicKey</c> instance with empty exponent and modulus values.
        /// </summary>
        public TaskAgentPublicKey()
        {
        }

        /// <summary>
        /// Initializes a new <c>TaskAgentPublicKey</c> instance with the specified exponent and modulus values.
        /// </summary>
        /// <param name="exponent">The exponent value of the key</param>
        /// <param name="modulus">The modulus value of the key</param>
        public TaskAgentPublicKey(
            Byte[] exponent,
            Byte[] modulus)
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(exponent, nameof(exponent));
            ArgumentUtility.CheckEnumerableForNullOrEmpty(modulus, nameof(modulus));

            this.Exponent = exponent;
            this.Modulus = modulus;
        }

        private TaskAgentPublicKey(TaskAgentPublicKey objectToBeCloned)
        {
            if (objectToBeCloned.Exponent != null)
            {
                this.Exponent = new Byte[objectToBeCloned.Exponent.Length];
                Buffer.BlockCopy(objectToBeCloned.Exponent, 0, this.Exponent, 0, objectToBeCloned.Exponent.Length);
            }

            if (objectToBeCloned.Modulus != null)
            {
                this.Modulus = new Byte[objectToBeCloned.Modulus.Length];
                Buffer.BlockCopy(objectToBeCloned.Modulus, 0, this.Modulus, 0, objectToBeCloned.Modulus.Length);
            }
        }

        /// <summary>
        /// Gets or sets the exponent for the public key.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Byte[] Exponent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the modulus for the public key.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Byte[] Modulus
        {
            get;
            set;
        }

        public TaskAgentPublicKey Clone()
        {
            return new TaskAgentPublicKey(this);
        }
    }
}
