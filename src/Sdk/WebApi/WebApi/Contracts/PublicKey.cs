using GitHub.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Represents the public key portion of an RSA asymmetric key.
    /// </summary>
    [DataContract]
    public class PublicKey
    {
        /// <summary>
        /// Initializes a new <c>PublicKey</c> instance with empty exponent and modulus values.
        /// </summary>
        public PublicKey()
        {
        }

        /// <summary>
        /// Initializes a new <c>TaskAgentPublicKey</c> instance with the specified exponent and modulus values.
        /// </summary>
        /// <param name="exponent">The exponent value of the key</param>
        /// <param name="modulus">The modulus value of the key</param>
        public PublicKey(
            Byte[] exponent,
            Byte[] modulus)
        {
            ArgumentUtility.CheckEnumerableForNullOrEmpty(exponent, nameof(exponent));
            ArgumentUtility.CheckEnumerableForNullOrEmpty(modulus, nameof(modulus));

            this.Exponent = exponent;
            this.Modulus = modulus;
        }

        private PublicKey(PublicKey objectToBeCloned)
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

        public PublicKey Clone()
        {
            return new PublicKey(this);
        }
    }
}
