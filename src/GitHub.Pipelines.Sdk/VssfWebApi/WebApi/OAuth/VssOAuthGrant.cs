using System;
using System.Collections.Generic;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Represents an authorization grant in an OAuth 2.0 token exchange.
    /// </summary>
    public abstract class VssOAuthGrant : IVssOAuthTokenParameterProvider
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthGrant</c> instance with the specified grant type.
        /// </summary>
        /// <param name="grantType">The type of authorization grant</param>
        protected VssOAuthGrant(VssOAuthGrantType grantType)
        {
            m_grantType = grantType;
        }

        /// <summary>
        /// Gets the type of authorization grant.
        /// </summary>
        public VssOAuthGrantType GrantType
        {
            get
            {
                return m_grantType;
            }
        }

        /// <summary>
        /// Gets the client credentials authorization grant.
        /// </summary>
        public static VssOAuthClientCredentialsGrant ClientCredentials
        {
            get
            {
                return s_clientCredentialsGrant.Value;
            }
        }

        /// <summary>
        /// When overridden in a derived class, the corresponding token request parameters should be set for the 
        /// grant type represented by the instance.
        /// </summary>
        /// <param name="parameters">The parameters to post to an authorization server</param>
        protected abstract void SetParameters(IDictionary<String, String> parameters);

        void IVssOAuthTokenParameterProvider.SetParameters(IDictionary<String, String> parameters)
        {
            SetParameters(parameters);
        }

        private readonly VssOAuthGrantType m_grantType;
        private static readonly Lazy<VssOAuthClientCredentialsGrant> s_clientCredentialsGrant = new Lazy<VssOAuthClientCredentialsGrant>(() => new VssOAuthClientCredentialsGrant());
    }
}
