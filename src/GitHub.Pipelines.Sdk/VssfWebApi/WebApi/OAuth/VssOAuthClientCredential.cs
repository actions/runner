using System;
using System.Collections.Generic;
using GitHub.Services.Common;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Provides client credentials for proof of identity in OAuth 2.0 token exchanges.
    /// </summary>
    public abstract class VssOAuthClientCredential : IVssOAuthTokenParameterProvider, IDisposable
    {
        protected VssOAuthClientCredential(
            VssOAuthClientCredentialType type,
            String clientId)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(clientId, nameof(clientId));

            m_type = type;
            m_clientId = clientId;
        }

        /// <summary>
        /// Gets the client identifier.
        /// </summary>
        public String ClientId
        {
            get
            {
                return m_clientId;
            }
        }

        /// <summary>
        /// Gets the type of credentials for this instance.
        /// </summary>
        public VssOAuthClientCredentialType CredentialType
        {
            get
            {
                return m_type;
            }
        }

        /// <summary>
        /// Disposes of managed resources referenced by the credentials.
        /// </summary>
        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            Dispose(true);
        }

        protected virtual void Dispose(Boolean disposing)
        {     
        }

        /// <summary>
        /// When overridden in a derived class, the corresponding token request parameters should be set for the 
        /// credential type represented by the instance.
        /// </summary>
        /// <param name="parameters">The parameters to post to an authorization server</param>
        protected abstract void SetParameters(IDictionary<String, String> parameters);

        void IVssOAuthTokenParameterProvider.SetParameters(IDictionary<String, String> parameters)
        {
            SetParameters(parameters);
        }

        private Boolean m_disposed;
        private readonly String m_clientId;
        private readonly VssOAuthClientCredentialType m_type;
    }
}
