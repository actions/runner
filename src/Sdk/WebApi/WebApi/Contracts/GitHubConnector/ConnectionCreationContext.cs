using System;

namespace GitHub.Services.GitHubConnector
{
    public class ConnectionCreationContext
    {
        public string GitHubInstallationId { get; set; }

        public string GitHubUserOAuthCode { get; set; }
    }
}
