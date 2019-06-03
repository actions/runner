using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace Runner.Sdk
{
    public class AgentWebProxySettings
    {
        public string ProxyAddress { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public List<string> ProxyBypassList { get; set; }
        public IWebProxy WebProxy { get; set; }
    }

    public class AgentWebProxy : IWebProxy
    {
        private string _proxyAddress;
        private readonly List<Regex> _regExBypassList = new List<Regex>();

        public ICredentials Credentials { get; set; }

        public AgentWebProxy()
        {
        }

        public AgentWebProxy(string proxyAddress, string proxyUsername, string proxyPassword, List<string> proxyBypassList)
        {
            Update(proxyAddress, proxyUsername, proxyPassword, proxyBypassList);
        }

        public void Update(string proxyAddress, string proxyUsername, string proxyPassword, List<string> proxyBypassList)
        {
            _proxyAddress = proxyAddress?.Trim();

            if (string.IsNullOrEmpty(proxyUsername) || string.IsNullOrEmpty(proxyPassword))
            {
                Credentials = CredentialCache.DefaultNetworkCredentials;
            }
            else
            {
                Credentials = new NetworkCredential(proxyUsername, proxyPassword);
            }

            if (proxyBypassList != null)
            {
                foreach (string bypass in proxyBypassList)
                {
                    if (string.IsNullOrWhiteSpace(bypass))
                    {
                        continue;
                    }
                    else
                    {
                        try
                        {
                            Regex bypassRegex = new Regex(bypass.Trim(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ECMAScript);
                            _regExBypassList.Add(bypassRegex);
                        }
                        catch (Exception)
                        {
                            // eat all exceptions
                        }
                    }
                }
            }
        }

        public Uri GetProxy(Uri destination)
        {
            if (IsBypassed(destination))
            {
                return destination;
            }
            else
            {
                return new Uri(_proxyAddress);
            }
        }

        public bool IsBypassed(Uri uri)
        {
            return string.IsNullOrEmpty(_proxyAddress) || uri.IsLoopback || IsMatchInBypassList(uri);
        }

        private bool IsMatchInBypassList(Uri input)
        {
            string matchUriString = input.IsDefaultPort ?
                input.Scheme + "://" + input.Host :
                input.Scheme + "://" + input.Host + ":" + input.Port.ToString();

            foreach (Regex r in _regExBypassList)
            {
                if (r.IsMatch(matchUriString))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
