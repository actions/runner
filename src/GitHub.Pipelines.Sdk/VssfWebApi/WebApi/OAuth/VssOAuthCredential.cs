using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using GitHub.Services.Common;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Provides authentication with a secure token service using the OAuth 2.0 protocol.
    /// </summary>
    public class VssOAuthCredential : FederatedCredential
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthCredential</c> instance with the specified authorization grant and client 
        /// credentials.
        /// </summary>
        /// <param name="authorizationUrl">The location of the token endpoint for the target authorization server</param>
        /// <param name="grant">The grant to provide for the token exchange</param>
        /// <param name="clientCredential">The client credentials to provide for the token exchange</param>
        /// <param name="tokenParameters">An optional set of token parameters which, if present, are sent in the request body of the token request</param>
        /// <param name="accessToken">An optional access token which, if present, is used prior to requesting new tokens</param>
        public VssOAuthCredential(
            Uri authorizationUrl,
            VssOAuthGrant grant,
            VssOAuthClientCredential clientCredential,
            VssOAuthTokenParameters tokenParameters = null,
            VssOAuthAccessToken accessToken = null)
            : base(accessToken)
        {
            ArgumentUtility.CheckForNull(authorizationUrl, nameof(authorizationUrl));
            ArgumentUtility.CheckForNull(grant, nameof(grant));

            m_authorizationUrl = authorizationUrl;
            m_grant = grant;
            m_tokenParameters = tokenParameters;
            m_clientCredential = clientCredential;
        }

        /// <summary>
        /// Gets the type of issued token credential.
        /// </summary>
        public override VssCredentialsType CredentialType
        {
            get
            {
                return VssCredentialsType.OAuth;
            }
        }

        /// <summary>
        /// Gets the authorization endpoint for this credential.
        /// </summary>
        public Uri AuthorizationUrl
        {
            get
            {
                return m_authorizationUrl;
            }
        }

        /// <summary>
        /// Gets the grant for this credential.
        /// </summary>
        public VssOAuthGrant Grant
        {
            get
            {
                return m_grant;
            }
        }

        /// <summary>
        /// Gets the client credentials for this credential.
        /// </summary>
        public VssOAuthClientCredential ClientCredential
        {
            get
            {
                return m_clientCredential;
            }
        }

        /// <summary>
        /// Gets the set of additional token parameters configured for the credential.
        /// </summary>
        public VssOAuthTokenParameters TokenParameters
        {
            get
            {
                if (m_tokenParameters == null)
                {
                    m_tokenParameters = new VssOAuthTokenParameters();
                }
                return m_tokenParameters;
            }
        }

        /// <summary>
        /// Determines whether or not the response reperesents an authentication challenge for the current credential.
        /// </summary>
        /// <param name="webResponse">The response to analyze</param>
        /// <returns>True if the web response indicates an authorization challenge; otherwise, false</returns>
        public override Boolean IsAuthenticationChallenge(IHttpResponse webResponse)
        {
            if (webResponse == null)
            {
                return false;
            }

            if (webResponse.StatusCode == HttpStatusCode.Found ||
                webResponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                return webResponse.Headers.GetValues(Common.Internal.HttpHeaders.WwwAuthenticate).Any(x => x.IndexOf("Bearer", StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return false;
        }

        protected override IssuedTokenProvider OnCreateTokenProvider(
            Uri serverUrl, 
            IHttpResponse response)
        {
            return new VssOAuthTokenProvider(this, serverUrl);
        }

        private VssOAuthTokenParameters m_tokenParameters;

        private readonly Uri m_authorizationUrl;
        private readonly VssOAuthGrant m_grant;
        private readonly VssOAuthClientCredential m_clientCredential;
    }
}
