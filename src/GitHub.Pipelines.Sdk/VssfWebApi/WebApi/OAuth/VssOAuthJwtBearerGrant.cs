using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.OAuth
{
    /// <summary>
    /// Implements the JWT Bearer Token Grant Type Profile for OAuth 2.0.
    /// </summary>
    public sealed class VssOAuthJwtBearerGrant : VssOAuthGrant
    {
        /// <summary>
        /// Intializes a new <c>VssOAuthJwtBearerGrant</c> instance with the specified issuer, subject,
        /// audience, and signing credentials.
        /// </summary>
        /// <param name="issuer">The issuer of the grant. This is typically the client identifier</param>
        /// <param name="subject">The subject of the grant. This is an identifier for the target user of the access token</param>
        /// <param name="audience">The audience of the grant. This is typically the authorization URL</param>
        /// <param name="signingCredentials">The signing credentials for proof of client identity</param>
        public VssOAuthJwtBearerGrant(
            String issuer,
            String subject,
            String audience,
            VssSigningCredentials signingCredentials)
            : this(new VssOAuthJwtBearerAssertion(issuer, subject, audience, signingCredentials))
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthJwtBearerGrant</c> instance with the specified assertion.
        /// </summary>
        /// <param name="assertion">The jwt-bearer assertion for the authorization grant</param>
        public VssOAuthJwtBearerGrant(VssOAuthJwtBearerAssertion assertion)
            : base(VssOAuthGrantType.JwtBearer)
        {
            ArgumentUtility.CheckForNull(assertion, nameof(assertion));

            m_assertion = assertion;
        }

        protected override void SetParameters(IDictionary<String, String> parameters)
        {
            parameters[VssOAuthConstants.GrantType] = VssOAuthConstants.JwtBearerAuthorizationGrantType;
            parameters[VssOAuthConstants.Assertion] = m_assertion.GetBearerToken().EncodedToken;
        }

        private readonly VssOAuthJwtBearerAssertion m_assertion;
    }
}
