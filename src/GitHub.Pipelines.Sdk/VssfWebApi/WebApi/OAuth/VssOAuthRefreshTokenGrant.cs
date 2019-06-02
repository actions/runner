using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Common;

namespace Microsoft.VisualStudio.Services.OAuth
{
    /// <summary>
    /// Represents an OAuth 2.0 grant for refreshing access tokens.
    /// </summary>
    public sealed class VssOAuthRefreshTokenGrant : VssOAuthGrant
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthRefreshTokenGrant</c> instance with the specified token value.
        /// </summary>
        /// <param name="refreshToken">The refresh token provided by the authorization server</param>
        public VssOAuthRefreshTokenGrant(String refreshToken) 
            : base(VssOAuthGrantType.RefreshToken)
        {
            ArgumentUtility.CheckStringForNullOrEmpty(refreshToken, nameof(refreshToken));

            m_refreshToken = refreshToken;
        }

        /// <summary>
        /// Gets the token used for acquiring a new access token from the authorization server.
        /// </summary>
        public String RefreshToken
        {
            get
            {
                return m_refreshToken;
            }
        }

        protected override void SetParameters(IDictionary<String, String> parameters)
        {
            parameters[VssOAuthConstants.GrantType] = VssOAuthConstants.RefreshTokenGrantType;
            parameters[VssOAuthConstants.RefreshToken] = m_refreshToken;    // this matches RFC6749
            parameters[VssOAuthConstants.Assertion] = m_refreshToken;       // this matches our OAuthController and RFC
        }

        private readonly String m_refreshToken;
    }
}
