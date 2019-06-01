using System;

namespace Microsoft.VisualStudio.Services.GitHubConnector
{
    public class InstallationToken
    {
        public string AccessToken { get; set; }

        public DateTime ExpiryTime { get; set; }
    }
}
