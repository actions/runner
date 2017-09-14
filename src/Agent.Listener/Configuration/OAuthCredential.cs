using System;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
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

            ArgUtil.NotNullOrEmpty(clientId, nameof(clientId));
            ArgUtil.NotNullOrEmpty(authorizationUrl, nameof(authorizationUrl));

            // For TFS, we need make sure the Schema/Host/Port component of the authorization url also match configuration url.
            // We can't do this for VSTS, since its SPS/TFS urls are different.
            var configStore = context.GetService<IConfigurationStore>();
            if (configStore.IsConfigured())
            {
                UriBuilder configServerUrl = new UriBuilder(configStore.GetSettings().ServerUrl);
                UriBuilder authorizationUrlBuilder = new UriBuilder(authorizationUrl);
                if (!UrlUtil.IsHosted(configServerUrl.Uri.AbsoluteUri) &&
                    Uri.Compare(configServerUrl.Uri, authorizationUrlBuilder.Uri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    authorizationUrlBuilder.Scheme = configServerUrl.Scheme;
                    authorizationUrlBuilder.Host = configServerUrl.Host;
                    authorizationUrlBuilder.Port = configServerUrl.Port;

                    var trace = context.GetTrace(nameof(OAuthCredential));
                    trace.Info($"Replace authorization url's scheme://host:port component with agent configure url's scheme://host:port: '{authorizationUrlBuilder.Uri.AbsoluteUri}'.");

                    authorizationUrl = authorizationUrlBuilder.Uri.AbsoluteUri;
                }
            }

            // We expect the key to be in the machine store at this point. Configuration should have set all of
            // this up correctly so we can use the key to generate access tokens.
            var keyManager = context.GetService<IRSAKeyManager>();
            var signingCredentials = VssSigningCredentials.Create(() => keyManager.GetKey());
            var clientCredential = new VssOAuthJwtBearerClientCredential(clientId, authorizationUrl, signingCredentials);
            var agentCredential = new VssOAuthCredential(new Uri(authorizationUrl, UriKind.Absolute), VssOAuthGrant.ClientCredentials, clientCredential);

            // Construct a credentials cache with a single OAuth credential for communication. The windows credential
            // is explicitly set to null to ensure we never do that negotiation.
            return new VssCredentials(null, agentCredential, CredentialPromptType.DoNotPrompt);
        }
    }
}
