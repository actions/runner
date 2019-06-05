using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using GitHub.Services.Common;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Implements the Client Password Profile for OAuth 2.0 Client Authentication.
    /// </summary>
    public sealed class VssOAuthPasswordClientCredential : VssOAuthClientCredential
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthPasswordClientCredential</c> with the specified client identifier and secret.
        /// </summary>
        /// <param name="clientId">The client identifier issued by the authorization server</param>
        /// <param name="clientSecret">The client secret/password issued by the authorization server</param>
        public VssOAuthPasswordClientCredential(
            String clientId,
            String clientSecret)
            : this(clientId, EncryptSecret(clientSecret))
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthPasswordClientCredential</c> with the specified client identifier and secret.
        /// </summary>
        /// <param name="clientId">The client identifier issued by the authorization server</param>
        /// <param name="clientSecret">The client secret/password issued by the authorization server</param>
        public VssOAuthPasswordClientCredential(
            String clientId,
            SecureString clientSecret)
            : base(VssOAuthClientCredentialType.Password, clientId)
        {
            ArgumentUtility.CheckForNull(clientSecret, nameof(clientSecret));

            m_clientSecret = clientSecret;
        }

        private static SecureString EncryptSecret(String value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            var secureString = new SecureString();
            foreach (var character in value)
            {
                secureString.AppendChar(character);
            }
            secureString.MakeReadOnly();
            return secureString;
        }

        private static String DecryptSecret(SecureString value)
        {
            if (value == null || value.Length == 0)
            {
                return null;
            }

            IntPtr bStr = IntPtr.Zero;
            try
            {
                bStr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(bStr);
            }
            finally
            {
                if (bStr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(bStr);
                }
            }
        }

        protected override void SetParameters(IDictionary<String, String> parameters)
        {
            parameters[VssOAuthConstants.ClientId] = this.ClientId;
            parameters[VssOAuthConstants.ClientSecret] = DecryptSecret(m_clientSecret);
        }

        private readonly SecureString m_clientSecret;
    }
}
