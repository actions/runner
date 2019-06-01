using System;

namespace Microsoft.VisualStudio.Services.GitHubConnector
{
    public class ConnectionCreationContext
    {
        public string GitHubInstallationId { get; set; }

        public string GitHubUserOAuthCode { get; set; }
    }
}
