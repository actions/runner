using System;
using System.Collections.Generic;
using GitHub.Services.Common;
using GitHub.Services.WebApi;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Implements the JWT Bearer Token Profile for OAuth 2.0 Client Authentication.
    /// </summary>
    public sealed class VssOAuthJwtBearerClientCredential : VssOAuthClientCredential
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthJwtBearerClientCredential</c> with the specified client identifier and audience. The
        /// credential will be used for the JWT Bearer Token Profile for Client Authentication as a client assertion.
        /// </summary>
        /// <param name="clientId">The client identifier issued by the authorization server</param>
        /// <param name="audience">The target audience for the bearer assertion. This is usually the authorization URL</param>
        /// <param name="signingCredentials">The signing credentials for proof of client identity</param>
        public VssOAuthJwtBearerClientCredential(
            String clientId,
            String audience,
            VssSigningCredentials signingCredentials)
            : this(clientId, new VssOAuthJwtBearerAssertion(clientId, clientId, audience, signingCredentials))
        {
        }

        /// <summary>
        /// Initializes a new <c>VssOAuthJwtBearerClientCredential</c> with the specified JWT bearer assertion.
        /// </summary>
        /// <param name="clientId">The client identifier issued by the authorization server</param>
        /// <param name="assertion">The client assertion for proof of identity</param>
        public VssOAuthJwtBearerClientCredential(
            String clientId,
            VssOAuthJwtBearerAssertion assertion)
            : base(VssOAuthClientCredentialType.JwtBearer, clientId)
        {
            ArgumentUtility.CheckForNull(assertion, nameof(assertion));

            m_assertion = assertion;
        }

        /// <summary>
        /// Gets the jwt-bearer assertion for issuing tokens.
        /// </summary>
        public VssOAuthJwtBearerAssertion Assertion
        {
            get
            {
                return m_assertion;
            }
        }

        protected override void SetParameters(IDictionary<String, String> parameters)
        {
            parameters[VssOAuthConstants.ClientAssertionType] = VssOAuthConstants.JwtBearerClientAssertionType;
            parameters[VssOAuthConstants.ClientAssertion] = m_assertion.GetBearerToken().EncodedToken;
        }

        private readonly VssOAuthJwtBearerAssertion m_assertion;
    }
}
