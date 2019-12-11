using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        /// Verifies the signature of the input data, returning true if the signature is valid.
        /// </summary>
        /// <param name="input">The data which should be signed</param>
        /// <param name="signature">The signature which should be verified</param>
        /// <returns>True if the provided signature matches the current signing token; otherwise, false</returns>
        public abstract Boolean VerifySignature(Byte[] input, Byte[] signature);

        /// <summary>
        /// Creates a new <c>VssSigningCredentials</c> instance using the specified <paramref name="certificate"/> instance
        /// as the signing key.
        /// </summary>
        /// <param name="certificate">The certificate which contains the key used for signing and verification</param>
        /// <returns>A new <c>VssSigningCredentials</c> instance which uses the specified certificate for signing</returns>
        public static VssSigningCredentials Create(X509Certificate2 certificate)
        {
            ArgumentUtility.CheckForNull(certificate, nameof(certificate));

            if (certificate.HasPrivateKey)
            {
                var rsa = certificate.GetRSAPrivateKey();
                if (rsa == null)
                {
                    throw new SignatureAlgorithmUnsupportedException(certificate.SignatureAlgorithm.FriendlyName);
                }

                if (rsa.KeySize < c_minKeySize)
                {
                    throw new InvalidCredentialsException(JwtResources.SigningTokenKeyTooSmall());
                }
            }

            return new X509Certificate2SigningToken(certificate);
        }

        /// <summary>
        /// Creates a new <c>VssSigningCredentials</c> instance using the specified <paramref name="factory"/> 
        /// callback function to retrieve the signing key.
        /// </summary>
        /// <param name="factory">The factory which creates <c>RSA</c> keys used for signing and verification</param>
        /// <returns>A new <c>VssSigningCredentials</c> instance which uses the specified provider for signing</returns>
        public static VssSigningCredentials Create(Func<RSA> factory)
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

                return new RSASigningToken(factory, rsa.KeySize);
            }
        }

        /// <summary>
        /// Creates a new <c>VssSigningCredentials</c> instance using the specified <paramref name="key"/> as the signing 
        /// key. The returned signing token performs symmetric key signing and verification.
        /// </summary>
        /// <param name="rsa">The key used for signing and verification</param>
        /// <returns>A new <c>VssSigningCredentials</c> instance which uses the specified key for signing</returns>
        public static VssSigningCredentials Create(Byte[] key)
        {
            ArgumentUtility.CheckForNull(key, nameof(key));

            // Probably should have validation here, but there was none previously
            return new SymmetricKeySigningToken(key);
        }

        private const Int32 c_minKeySize = 2048;
        private readonly DateTime m_effectiveDate;

#region Concrete Implementations

        private class SymmetricKeySigningToken : VssSigningCredentials
        {
            public SymmetricKeySigningToken(Byte[] key)
            {
                m_key = new Byte[key.Length];
                Buffer.BlockCopy(key, 0, m_key, 0, m_key.Length);
            }

            public override Boolean CanSignData
            {
                get
                {
                    return true;
                }
            }

            public override Int32 KeySize
            {
                get
                {
                    return m_key.Length * 8;
                }
            }

            public override JWTAlgorithm SignatureAlgorithm
            {
                get
                {
                    return JWTAlgorithm.HS256;
                }
            }

            protected override Byte[] GetSignature(Byte[] input)
            {
                using (var hash = new HMACSHA256(m_key))
                {
                    return hash.ComputeHash(input);
                }
            }

            public override Boolean VerifySignature(
                Byte[] input,
                Byte[] signature)
            {
                var computedSignature = SignData(input);
                return SecureCompare.TimeInvariantEquals(computedSignature, signature);
            }

            private readonly Byte[] m_key;
        }

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

        private class X509Certificate2SigningToken : AsymmetricKeySigningToken, IJsonWebTokenHeaderProvider
        {
            public X509Certificate2SigningToken(X509Certificate2 certificate)
            {
                m_certificate = certificate;
            }

            public override Int32 KeySize
            {
                get
                {
                    return m_certificate.GetRSAPublicKey().KeySize;
                }
            }

            public override DateTime ValidFrom
            {
                get
                {
                    return m_certificate.NotBefore;
                }
            }

            public override DateTime ValidTo
            {
                get
                {
                    return m_certificate.NotAfter;
                }
            }

            public override Boolean VerifySignature(
                Byte[] input,
                Byte[] signature)
            {
                var rsa = m_certificate.GetRSAPublicKey();
                return rsa.VerifyData(input, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            protected override Byte[] GetSignature(Byte[] input)
            {
                var rsa = m_certificate.GetRSAPrivateKey();
                return rsa.SignData(input, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            protected override Boolean HasPrivateKey()
            {
                return m_certificate.HasPrivateKey;
            }

            void IJsonWebTokenHeaderProvider.SetHeaders(IDictionary<String, Object> headers)
            {
                headers[JsonWebTokenHeaderParameters.X509CertificateThumbprint] = m_certificate.GetCertHash().ToBase64StringNoPadding();
            }

            private readonly X509Certificate2 m_certificate;
        }

        private class RSASigningToken : AsymmetricKeySigningToken
        {
            public RSASigningToken(
                Func<RSA> factory,
                Int32 keySize)
            {
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
                    return rsa.SignData(input, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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

            public override Boolean VerifySignature(
                Byte[] input,
                Byte[] signature)
            {
                using (var rsa = m_factory())
                {
                    return rsa.VerifyData(input, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                }
            }

            private readonly Int32 m_keySize;
            private readonly Func<RSA> m_factory;
        }

        #endregion
    }
}
