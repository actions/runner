using System;

namespace GitHub.Runner.Sdk
{
    public static class UrlUtil
    {
        public static bool IsHostedServer(UriBuilder gitHubUrl)
        {
            if (StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_FORCE_GHES")))
            {
                return false;
            }

            return
                string.Equals(gitHubUrl.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gitHubUrl.Host, "www.github.com", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(gitHubUrl.Host, "github.localhost", StringComparison.OrdinalIgnoreCase) ||
                gitHubUrl.Host.EndsWith(".ghe.localhost", StringComparison.OrdinalIgnoreCase) ||
                gitHubUrl.Host.EndsWith(".ghe.com", StringComparison.OrdinalIgnoreCase);
        }

        public static Uri GetCredentialEmbeddedUrl(Uri baseUrl, string username, string password)
        {
            ArgUtil.NotNull(baseUrl, nameof(baseUrl));

            // return baseurl when there is no username and password
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            {
                return baseUrl;
            }

            UriBuilder credUri = new(baseUrl);

            // ensure we have a username, uribuild will throw if username is empty but password is not.
            if (string.IsNullOrEmpty(username))
            {
                username = "emptyusername";
            }

            // escape chars in username for uri
            credUri.UserName = Uri.EscapeDataString(username);

            // escape chars in password for uri
            if (!string.IsNullOrEmpty(password))
            {
                credUri.Password = Uri.EscapeDataString(password);
            }

            return credUri.Uri;
        }
    }
}
