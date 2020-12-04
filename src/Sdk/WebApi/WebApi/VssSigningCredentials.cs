using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Jwt;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Provides a contract for signing, and verifying signatures of, blobs of data.
    /// </summary>
    public abstract class VssSigningCredentials
    {
        protected VssSigningCredentials()
        {
            m_effectiveDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets a value indicating whether or not this token may be used to sign data.
        /// </summary>
        public abstract Boolean CanSignData
        {
            get;
        }

        /// <summary>
        /// Gets the size of the key, in bits, used for signing and verification.
        /// </summary>
        public abstract Int32 KeySize
        {
            get;
        }

        /// <summary>
        /// Gets the date from which this signing token is valid.
        /// </summary>
        public virtual DateTime ValidFrom
        {
            get
            {
                return m_effectiveDate;
            }
        }

        /// <summary>
        /// Gets the datetime at which this signing token expires.
        /// </summary>
        public virtual DateTime ValidTo
        {
            get
            {
                return DateTime.MaxValue;
            }
        }

        /// <summary>
        /// Gets the signature algorithm used by this signing token.
        /// </summary>
        public abstract JWTAlgorithm SignatureAlgorithm
        {
            get;
        }

        /// <summary>
        /// Signs the <paramref name="input"/> array with the signing key associated with the token.
        /// </summary>
        /// <param name="input">The data which should be signed</param>
        /// <returns>A blob of data representing the signature of the input data</returns>
        /// <exception cref="InvalidOperationException">Thrown when the current instance cannot be used for signing</exception>
        public virtual Byte[] SignData(Byte[] input)
        {
            if (!CanSignData)
            {
                throw new InvalidOperationException();
            }
            return GetSignature(input);
        }

        /// <summary>
        /// Signs the <paramref name="input"/> array with the signing key associated with the token.
        /// </summary>
        /// <param name="input">The data which should be signed</param>
        /// <returns>A blob of data representing the signature of the input data</returns>
        protected abstract Byte[] GetSignature(Byte[] input);

        /// <summary>
        /// Creates a new <c>VssSigningCredentials</c> instance using the specified <paramref name="factory"/> 
        /// callback function to retrieve the signing key.
        /// </summary>
        /// <param name="factory">The factory which creates <c>RSA</c> keys used for signing and verification</param>
        /// <returns>A new <c>VssSigningCredentials</c> instance which uses the specified provider for signing</returns>
        public static VssSigningCredentials Create(Func<RSA> factory, bool requireFipsCryptography)
        {
            ArgumentUtility.CheckForNull(factory, nameof(factory));

            using (var rsa = factory())
            {
                if (rsa == null)
                {
                    throw new InvalidCredentialsException(JwtResources.SignatureAlgorithmUnsupportedException("None"));
                }

                if (rsa.KeySize < c_minKeySize)
                {
                    throw new InvalidCredentialsException(JwtResources.SigningTokenKeyTooSmall());
                }

                if (requireFipsCryptography)
                {
                    return new RSASigningToken(factory, rsa.KeySize, RSASignaturePadding.Pss);
                }
                return new RSASigningToken(factory, rsa.KeySize, RSASignaturePadding.Pkcs1);
            }
        }

        private const Int32 c_minKeySize = 2048;
        private readonly DateTime m_effectiveDate;

#region Concrete Implementations

        private abstract class AsymmetricKeySigningToken : VssSigningCredentials
        {
            protected abstract Boolean HasPrivateKey();

            public override JWTAlgorithm SignatureAlgorithm
            {
                get
                {
                    return JWTAlgorithm.RS256;
                }
            }

            public override Boolean CanSignData
            {
                get
                {
                    if (m_hasPrivateKey == null)
                    {
                        m_hasPrivateKey = HasPrivateKey();
                    }
                    return m_hasPrivateKey.Value;
                }
            }

            private Boolean? m_hasPrivateKey;
        }

        private class RSASigningToken : AsymmetricKeySigningToken
        {
            public RSASigningToken(
                Func<RSA> factory,
                Int32 keySize,
                RSASignaturePadding signaturePadding)
            {
                m_signaturePadding = signaturePadding;
                m_keySize = keySize;
                m_factory = factory;
            }

            public override Int32 KeySize
            {
                get
                {
                    return m_keySize;
                }
            }

            protected override Byte[] GetSignature(Byte[] input)
            {
                using (var rsa = m_factory())
                {
                    return rsa.SignData(input, HashAlgorithmName.SHA256, m_signaturePadding);
                }
            }

            protected override Boolean HasPrivateKey()
            {
                try
                {
                    // As unfortunate as this is, there is no way to tell from an RSA implementation, based on querying
                    // properties alone, if it supports signature operations or has a private key. This is a one-time
                    // hit for the signing credentials implementation, so it shouldn't be a huge deal.
                    GetSignature(new Byte[1] { 1 });
                    return true;
                }
                catch (CryptographicException)
                {
                    return false;
                }
            }

            private readonly Int32 m_keySize;
            private readonly Func<RSA> m_factory;
            private readonly RSASignaturePadding m_signaturePadding;
        }

        #endregion
    }
}
