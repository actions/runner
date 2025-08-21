using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

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

        // For GitHub Enterprise Cloud with data residency, we allow fallback to GitHub.com for Actions resolution
        public static bool IsGHECDRFallbackToDotcom(UriBuilder gitHubUrl, ActionDownloadInfo downloadInfo)
        {
            string pattern = @"^https?:\/\/api\.github\.com\/repos\/[^\/]+\/[^\/]+\/(tar|zip)ball\/[a-zA-Z0-9._\/-]+$";
#if OS_WINDOWS
            if (downloadInfo.ZipballUrl != null && !Regex.IsMatch(downloadInfo.ZipballUrl.ToString(), pattern))
#else
            if (downloadInfo.TarballUrl != null && !Regex.IsMatch(downloadInfo.TarballUrl.ToString(), pattern))
#endif
            {
                return false;
            }

            if (gitHubUrl.Host.EndsWith(".ghe.localhost", StringComparison.OrdinalIgnoreCase) ||
                gitHubUrl.Host.EndsWith(".ghe.com", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
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

        public static string GetGitHubRequestId(HttpResponseHeaders headers)
        {
            if (headers != null &&
                headers.TryGetValues("x-github-request-id", out var headerValues))
            {
                return headerValues.FirstOrDefault();
            }
            return string.Empty;
        }

        public static string GetVssRequestId(HttpResponseHeaders headers)
        {
            if (headers != null &&
                headers.TryGetValues("x-vss-e2eid", out var headerValues))
            {
                return headerValues.FirstOrDefault();
            }
            return string.Empty;
        }
    }
}
