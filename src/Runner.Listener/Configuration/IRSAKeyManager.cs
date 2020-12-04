using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using GitHub.Runner.Common;

namespace GitHub.Runner.Listener.Configuration
{
    /// <summary>
    /// Manages an RSA key for the runner using the most appropriate store for the target platform.
    /// </summary>
#if OS_WINDOWS
    [ServiceLocator(Default = typeof(RSAEncryptedFileKeyManager))]
#else
    [ServiceLocator(Default = typeof(RSAFileKeyManager))]
#endif
    public interface IRSAKeyManager : IRunnerService
    {
        /// <summary>
        /// Creates a new <c>RSACryptoServiceProvider</c> instance for the current runner. If a key file is found then the current
        /// key is returned to the caller.
        /// </summary>
        /// <returns>An <c>RSACryptoServiceProvider</c> instance representing the key for the runner</returns>
        RSA CreateKey();

        /// <summary>
        /// Deletes the RSA key managed by the key manager.
        /// </summary>
        void DeleteKey();

        /// <summary>
        /// Gets the <c>RSACryptoServiceProvider</c> instance currently stored by the key manager. 
        /// </summary>
        /// <returns>An <c>RSACryptoServiceProvider</c> instance representing the key for the runner</returns>
        /// <exception cref="CryptographicException">No key exists in the store</exception>
        RSA GetKey();
    }

    // Newtonsoft 10 is not working properly with dotnet RSAParameters class
    // RSAParameters has fields marked as [NonSerialized] which cause we loss those fields after serialize to JSON
    // https://github.com/JamesNK/Newtonsoft.Json/issues/1517
    // https://github.com/dotnet/corefx/issues/23847
    // As workaround, we create our own RSAParameters class without any [NonSerialized] attributes.
    [Serializable]
    internal class RSAParametersSerializable : ISerializable
    {
        private RSAParameters _rsaParameters;

        public RSAParameters RSAParameters
        {
            get
            {
                return _rsaParameters;
            }
        }

        public RSAParametersSerializable(RSAParameters rsaParameters)
        {
            _rsaParameters = rsaParameters;
        }

        private RSAParametersSerializable()
        {
        }

        public byte[] D { get { return _rsaParameters.D; } set { _rsaParameters.D = value; } }

        public byte[] DP { get { return _rsaParameters.DP; } set { _rsaParameters.DP = value; } }

        public byte[] DQ { get { return _rsaParameters.DQ; } set { _rsaParameters.DQ = value; } }

        public byte[] Exponent { get { return _rsaParameters.Exponent; } set { _rsaParameters.Exponent = value; } }

        public byte[] InverseQ { get { return _rsaParameters.InverseQ; } set { _rsaParameters.InverseQ = value; } }

        public byte[] Modulus { get { return _rsaParameters.Modulus; } set { _rsaParameters.Modulus = value; } }

        public byte[] P { get { return _rsaParameters.P; } set { _rsaParameters.P = value; } }

        public byte[] Q { get { return _rsaParameters.Q; } set { _rsaParameters.Q = value; } }

        public RSAParametersSerializable(SerializationInfo information, StreamingContext context)
        {
            _rsaParameters = new RSAParameters()
            {
                D = (byte[])information.GetValue("d", typeof(byte[])),
                DP = (byte[])information.GetValue("dp", typeof(byte[])),
                DQ = (byte[])information.GetValue("dq", typeof(byte[])),
                Exponent = (byte[])information.GetValue("exponent", typeof(byte[])),
                InverseQ = (byte[])information.GetValue("inverseQ", typeof(byte[])),
                Modulus = (byte[])information.GetValue("modulus", typeof(byte[])),
                P = (byte[])information.GetValue("p", typeof(byte[])),
                Q = (byte[])information.GetValue("q", typeof(byte[]))
            };
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("d", _rsaParameters.D);
            info.AddValue("dp", _rsaParameters.DP);
            info.AddValue("dq", _rsaParameters.DQ);
            info.AddValue("exponent", _rsaParameters.Exponent);
            info.AddValue("inverseQ", _rsaParameters.InverseQ);
            info.AddValue("modulus", _rsaParameters.Modulus);
            info.AddValue("p", _rsaParameters.P);
            info.AddValue("q", _rsaParameters.Q);
        }
    }
}
