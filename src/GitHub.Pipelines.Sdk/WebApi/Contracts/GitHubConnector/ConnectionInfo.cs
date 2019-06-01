using System;

namespace Microsoft.VisualStudio.Services.GitHubConnector
{
    public class ConnectionInfo
    {
        public ConnectionInfo(Guid connectionId, string gitHubInstallationId, GitHubAccount gitHubAccount)
        {
            ConnectionId = connectionId;
            GitHubInstallationId = gitHubInstallationId;
            GitHubAccount = gitHubAccount;
        }

        public ConnectionInfo() { }

        public Guid ConnectionId { get; set; }

        public string GitHubInstallationId { get; set; }

        public GitHubAccount GitHubAccount { get; set; }
    }
}
