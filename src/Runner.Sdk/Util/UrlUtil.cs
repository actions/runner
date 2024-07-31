using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

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

        public static string GetGitHubRequestId(HttpResponseHeaders headers)
        {
            if (headers != null &&
                headers.TryGetValues("x-github-request-id", out var headerValues))
            {
                return headerValues.FirstOrDefault();
            }
            return string.Empty;
        }

        /// <summary>
        /// Uri.AbsoluteUri strips default port 80 (https://learn.microsoft.com/en-us/dotnet/api/system.uri)
        /// When the git runner command initiates, it infers default SOCKS(5) proxy port of 1080. This function
        /// optionally overrides the behavior of Uri.AbsoluteUri when port 80 is explicitly required.
        /// </summary>
        /// <param name="uri">The Uri object to convert to a string with port.</param>
        public static string GetAbsoluteUrlWithPort(Uri uri)
        {

            string userInfoClean = "";
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                string[] userInfoSegments = uri.UserInfo
                        .Split(":")
                        .Select(field => Uri.EscapeDataString(field))
                        .ToArray();
                userInfoClean = string.Join(":", userInfoSegments);
            }

            return string.Join("", new List<string> {
                uri.Scheme,             // http(s) 
                Uri.SchemeDelimiter,    // //:
                userInfoClean,          // user:pass
                string.IsNullOrEmpty(uri.UserInfo) ? "" : "@",
                uri.Host,               // sub.domain.com  
                ":",
                uri.Port.ToString(),    // :80
                uri.PathAndQuery        // path/page1
            });
        }
    }
}
