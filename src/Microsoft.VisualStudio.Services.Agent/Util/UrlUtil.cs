using System;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class UrlUtil
    {
        public static bool IsHosted(string serverUrl)
        {
            return serverUrl.IndexOf(".visualstudio.com", StringComparison.OrdinalIgnoreCase) != -1
                || serverUrl.IndexOf(".tfsallin.net", StringComparison.OrdinalIgnoreCase) != -1
                || serverUrl.IndexOf(".vsallin.net", StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static Uri GetCredentialEmbeddedUrl(Uri baseUrl, string username, string password)
        {
            ArgUtil.NotNull(baseUrl, nameof(baseUrl));

            // return baseurl when there is no username and password
            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password))
            {
                return baseUrl;
            }

            UriBuilder credUri = new UriBuilder(baseUrl);

            // escape chars in username for uri
            if (!string.IsNullOrEmpty(username))
            {
                credUri.UserName = Uri.EscapeDataString(username);
            }

            // escape chars in password for uri
            if (!string.IsNullOrEmpty(password))
            {
                credUri.Password = Uri.EscapeDataString(password);
            }

            return credUri.Uri;
        }
    }
}
