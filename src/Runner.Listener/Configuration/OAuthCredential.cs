using System;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;
using GitHub.Services.Common;
using GitHub.Services.OAuth;
using GitHub.Services.WebApi;

namespace GitHub.Runner.Listener.Configuration
{
    public class OAuthCredential : CredentialProvider
    {
        public OAuthCredential()
            : base(Constants.Configuration.OAuth)
        {
        }

        public override void EnsureCredential(
            IHostContext context,
            CommandSettings command,
            String serverUrl)
        {
            // Nothing to verify here
        }

        public override VssCredentials GetVssCredentials(IHostContext context)
        {
            var clientId = this.CredentialData.Data.GetValueOrDefault("clientId", null);
            var authorizationUrl = this.CredentialData.Data.GetValueOrDefault("authorizationUrl", null);

            // For back compat with .credential file that doesn't has 'oauthEndpointUrl' section
            var oauthEndpointUrl = this.CredentialData.Data.GetValueOrDefault("oauthEndpointUrl", authorizationUrl);

            ArgUtil.NotNullOrEmpty(clientId, nameof(clientId));
            ArgUtil.NotNullOrEmpty(authorizationUrl, nameof(authorizationUrl));

            // We expect the key to be in the machine store at this point. Configuration should have set all of
            // this up correctly so we can use the key to generate access tokens.
            var keyManager = context.GetService<IRSAKeyManager>();
            var signingCredentials = VssSigningCredentials.Create(() => keyManager.GetKey(), StringUtil.ConvertToBoolean(CredentialData.Data.GetValueOrDefault("requireFipsCryptography"), false));
            var clientCredential = new VssOAuthJwtBearerClientCredential(clientId, authorizationUrl, signingCredentials);
            var agentCredential = new VssOAuthCredential(new Uri(oauthEndpointUrl, UriKind.Absolute), VssOAuthGrant.ClientCredentials, clientCredential);

            // Construct a credentials cache with a single OAuth credential for communication. The windows credential
            // is explicitly set to null to ensure we never do that negotiation.
            return new VssCredentials(agentCredential, CredentialPromptType.DoNotPrompt);
        }
    }
}
