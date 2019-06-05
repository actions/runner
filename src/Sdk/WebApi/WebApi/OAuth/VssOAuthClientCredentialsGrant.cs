using System;
using System.Collections.Generic;

namespace GitHub.Services.OAuth
{
    /// <summary>
    /// Represents the client credentials grant for OAuth 2.0 token exchanges.
    /// </summary>
    public sealed class VssOAuthClientCredentialsGrant : VssOAuthGrant
    {
        /// <summary>
        /// Initializes a new <c>VssOAuthClientCredentials</c> grant.
        /// </summary>
        public VssOAuthClientCredentialsGrant()
            : base(VssOAuthGrantType.ClientCredentials)
        {
        }

        protected override void SetParameters(IDictionary<String, String> parameters)
        {
            parameters[VssOAuthConstants.GrantType] = VssOAuthConstants.ClientCredentialsGrantType;
        }
    }
}
