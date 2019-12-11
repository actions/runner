using GitHub.Services.Common;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Encapsulates the data used in an OAuth 2.0 token request.
    /// </summary>
    public class VssOAuthTokenRequest
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthTokenRequest</c> instance with the specified grant and client credential.
        /// </summary>
        /// <param name="grant">The authorization grant to use for the token request</param>
        /// <param name="clientCredential">The client credential to use for the token request</param>
        public VssOAuthTokenRequest(
            VssOAuthGrant grant,
            VssOAuthClientCredential clientCredential)
            : this(grant, clientCredential, null)
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthTokenRequest</c> instance with the specified grant and client credential. 
        /// Additional parameters specified by the token parameters will be provided in the token request.
        /// </summary>
        /// <param name="grant">The authorization grant to use for the token request</param>
        /// <param name="clientCredential">The client credential to use for the token request</param>
        /// <param name="tokenParameters">An optional set of additional parameters for the token request</param>
        public VssOAuthTokenRequest(
            VssOAuthGrant grant,
            VssOAuthClientCredential clientCredential,
            VssOAuthTokenParameters tokenParameters)
        {
            ArgumentUtility.CheckForNull(grant, nameof(grant));

            m_grant = grant;
            m_clientCredential = clientCredential;
            m_tokenParameters = tokenParameters;
        }

        /// <summary>
        /// Gets the authorization grant for this token request.
        /// </summary>
        public VssOAuthGrant Grant
        {
            get
            {
                return m_grant;
            }
        }

        /// <summary>
        /// Gets the client credential for this token request. Depending on the grant ype used, this value may be null.
        /// </summary>
        public VssOAuthClientCredential ClientCredential
        {
            get
            {
                return m_clientCredential;
            }
        }

        /// <summary>
        /// Gets the optional set of additional parameters for this token request.
        /// </summary>
        public VssOAuthTokenParameters Parameters
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

        private VssOAuthTokenParameters m_tokenParameters;

        private readonly VssOAuthGrant m_grant;
        private readonly VssOAuthClientCredential m_clientCredential;
    }
}
