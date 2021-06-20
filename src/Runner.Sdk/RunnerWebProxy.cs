using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace GitHub.Runner.Sdk
{
    public struct ByPassInfo
    {
        public string Host { get; set; }

        public string Port { get; set; }
    };

    public class RunnerWebProxy : IWebProxy
    {
        private string _httpProxyAddress;
        private string _httpProxyUsername;
        private string _httpProxyPassword;

        private string _httpsProxyAddress;
        private string _httpsProxyUsername;
        private string _httpsProxyPassword;
        private string _noProxyString;

        private readonly List<ByPassInfo> _noProxyList = new List<ByPassInfo>();
        private readonly HashSet<string> _noProxyUnique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Regex _validIpRegex = new Regex("^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$", RegexOptions.Compiled);

        public string HttpProxyAddress => _httpProxyAddress;
        public string HttpProxyUsername => _httpProxyUsername;
        public string HttpProxyPassword => _httpProxyPassword;

        public string HttpsProxyAddress => _httpsProxyAddress;
        public string HttpsProxyUsername => _httpsProxyUsername;
        public string HttpsProxyPassword => _httpsProxyPassword;
        public string NoProxyString => _noProxyString;

        public List<ByPassInfo> NoProxyList => _noProxyList;

        public ICredentials Credentials { get; set; }

        public RunnerWebProxy()
        {
            Credentials = new CredentialCache();

            var httpProxyAddress = Environment.GetEnvironmentVariable("http_proxy");
            if (string.IsNullOrEmpty(httpProxyAddress))
            {
                httpProxyAddress = Environment.GetEnvironmentVariable("HTTP_PROXY");
            }
            httpProxyAddress = httpProxyAddress?.Trim();

            var httpsProxyAddress = Environment.GetEnvironmentVariable("https_proxy");
            if (string.IsNullOrEmpty(httpsProxyAddress))
            {
                httpsProxyAddress = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            }
            httpsProxyAddress = httpsProxyAddress?.Trim();

            var noProxyList = Environment.GetEnvironmentVariable("no_proxy");
            if (string.IsNullOrEmpty(noProxyList))
            {
                noProxyList = Environment.GetEnvironmentVariable("NO_PROXY");
            }

            if (string.IsNullOrEmpty(httpProxyAddress) && string.IsNullOrEmpty(httpsProxyAddress))
            {
                return;
            }

            if (!string.IsNullOrEmpty(httpProxyAddress) && Uri.TryCreate(httpProxyAddress, UriKind.Absolute, out var proxyHttpUri))
            {
                _httpProxyAddress = proxyHttpUri.OriginalString;

                // Set both environment variables since there are tools support both casing (curl, wget) and tools support only one casing (docker)
                Environment.SetEnvironmentVariable("HTTP_PROXY", _httpProxyAddress);
                Environment.SetEnvironmentVariable("http_proxy", _httpProxyAddress);

                // the proxy url looks like http://[user:pass@]127.0.0.1:8888
                var userInfo = Uri.UnescapeDataString(proxyHttpUri.UserInfo).Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                if (userInfo.Length == 2)
                {
                    _httpProxyUsername = userInfo[0];
                    _httpProxyPassword = userInfo[1];
                }
                else if (userInfo.Length == 1)
                {
                    _httpProxyUsername = userInfo[0];
                }

                if (!string.IsNullOrEmpty(_httpProxyUsername) || !string.IsNullOrEmpty(_httpProxyPassword))
                {
                    var credentials = new NetworkCredential(_httpProxyUsername, _httpProxyPassword);

                    // Replace the entry in the credential cache if it exists
                    (Credentials as CredentialCache).Remove(proxyHttpUri, "Basic");
                    (Credentials as CredentialCache).Add(proxyHttpUri, "Basic", credentials);
                }
            }

            if (!string.IsNullOrEmpty(httpsProxyAddress) && Uri.TryCreate(httpsProxyAddress, UriKind.Absolute, out var proxyHttpsUri))
            {
                _httpsProxyAddress = proxyHttpsUri.OriginalString;

                // Set both environment variables since there are tools support both casing (curl, wget) and tools support only one casing (docker)
                Environment.SetEnvironmentVariable("HTTPS_PROXY", _httpsProxyAddress);
                Environment.SetEnvironmentVariable("https_proxy", _httpsProxyAddress);

                // the proxy url looks like http://[user:pass@]127.0.0.1:8888
                var userInfo = Uri.UnescapeDataString(proxyHttpsUri.UserInfo).Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                if (userInfo.Length == 2)
                {
                    _httpsProxyUsername = userInfo[0];
                    _httpsProxyPassword = userInfo[1];
                }
                else if (userInfo.Length == 1)
                {
                    _httpsProxyUsername = userInfo[0];
                }

                if (!string.IsNullOrEmpty(_httpsProxyUsername) || !string.IsNullOrEmpty(_httpsProxyPassword))
                {
                    var credentials = new NetworkCredential(_httpsProxyUsername, _httpsProxyPassword);

                    // Replace the entry in the credential cache if it exists
                    (Credentials as CredentialCache).Remove(proxyHttpsUri, "Basic");
                    (Credentials as CredentialCache).Add(proxyHttpsUri, "Basic", credentials);
                }
            }

            if (!string.IsNullOrEmpty(noProxyList))
            {
                _noProxyString = noProxyList;

                // Set both environment variables since there are tools support both casing (curl, wget) and tools support only one casing (docker)
                Environment.SetEnvironmentVariable("NO_PROXY", noProxyList);
                Environment.SetEnvironmentVariable("no_proxy", noProxyList);

                var noProxyListSplit = noProxyList.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (string noProxy in noProxyListSplit)
                {
                    var noProxyTrim = noProxy.Trim();
                    if (string.IsNullOrEmpty(noProxyTrim))
                    {
                        continue;
                    }
                    else if (_noProxyUnique.Add(noProxyTrim))
                    {
                        var noProxyInfo = new ByPassInfo();
                        var noProxyHostPort = noProxyTrim.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                        if (noProxyHostPort.Length == 1)
                        {
                            noProxyInfo.Host = noProxyHostPort[0];
                        }
                        else if (noProxyHostPort.Length == 2)
                        {
                            noProxyInfo.Host = noProxyHostPort[0];
                            noProxyInfo.Port = noProxyHostPort[1];
                        }

                        // We don't support IP address for no_proxy 
                        if (_validIpRegex.IsMatch(noProxyInfo.Host))
                        {
                            continue;
                        }

                        _noProxyList.Add(noProxyInfo);
                    }
                }
            }
        }

        public Uri GetProxy(Uri destination)
        {
            if (IsBypassed(destination))
            {
                return null;
            }

            if (destination.Scheme == Uri.UriSchemeHttps)
            {
                return new Uri(_httpsProxyAddress);
            }
            else
            {
                return new Uri(_httpProxyAddress);
            }
        }

        public bool IsBypassed(Uri uri)
        {
            if (uri.Scheme == Uri.UriSchemeHttps && string.IsNullOrEmpty(_httpsProxyAddress))
            {
                return true;
            }

            if (uri.Scheme == Uri.UriSchemeHttp && string.IsNullOrEmpty(_httpProxyAddress))
            {
                return true;
            }

            return uri.IsLoopback || IsUriInBypassList(uri);
        }

        private bool IsUriInBypassList(Uri input)
        {
            foreach (var noProxy in _noProxyList)
            {
                var matchHost = false;
                var matchPort = false;

                if (string.IsNullOrEmpty(noProxy.Port))
                {
                    matchPort = true;
                }
                else
                {
                    matchPort = string.Equals(noProxy.Port, input.Port.ToString());
                }

                if (noProxy.Host.StartsWith('.'))
                {
                    matchHost = input.Host.EndsWith(noProxy.Host, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    matchHost = string.Equals(input.Host, noProxy.Host, StringComparison.OrdinalIgnoreCase) || input.Host.EndsWith($".{noProxy.Host}", StringComparison.OrdinalIgnoreCase);
                }

                if (matchHost && matchPort)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
