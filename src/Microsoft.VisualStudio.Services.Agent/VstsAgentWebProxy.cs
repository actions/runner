using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Linq;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(VstsAgentWebProxy))]
    public interface IVstsAgentWebProxy : IAgentService, IWebProxy
    {
        string ProxyAddress { get; }
        string ProxyUsername { get; }
        string ProxyPassword { get; }
        List<string> ProxyBypassList { get; }
    }

    public class VstsAgentWebProxy : AgentService, IVstsAgentWebProxy, IWebProxy
    {
        private readonly List<Regex> _regExBypassList = new List<Regex>();
        private readonly List<string> _bypassList = new List<string>();

        public string ProxyAddress { get; private set; }
        public string ProxyUsername { get; private set; }
        public string ProxyPassword { get; private set; }
        public List<string> ProxyBypassList => _bypassList;

        public ICredentials Credentials { get; set; }

        public override void Initialize(IHostContext context)
        {
            base.Initialize(context);
            LoadProxySetting();
        }

        public Uri GetProxy(Uri destination)
        {
            if (IsBypassed(destination))
            {
                return destination;
            }
            else
            {
                return new Uri(ProxyAddress);
            }
        }

        public bool IsBypassed(Uri uri)
        {
            return string.IsNullOrEmpty(ProxyAddress) || uri.IsLoopback || IsMatchInBypassList(uri);
        }

        private void LoadProxySetting()
        {
            string proxyConfigFile = IOUtil.GetProxyConfigFilePath();
            if (File.Exists(proxyConfigFile))
            {
                // we expect the first line of the file is the proxy url
                Trace.Verbose($"Try read proxy setting from file: {proxyConfigFile}.");
                ProxyAddress = File.ReadLines(proxyConfigFile).FirstOrDefault() ?? string.Empty;
                ProxyAddress = ProxyAddress.Trim();
                Trace.Verbose($"{ProxyAddress}");
            }

            if (string.IsNullOrEmpty(ProxyAddress))
            {
                Trace.Verbose("Try read proxy setting from environment variable: 'VSTS_HTTP_PROXY'.");
                ProxyAddress = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY") ?? string.Empty;
                ProxyAddress = ProxyAddress.Trim();
                Trace.Verbose($"{ProxyAddress}");
            }

            if (!string.IsNullOrEmpty(ProxyAddress) && !Uri.IsWellFormedUriString(ProxyAddress, UriKind.Absolute))
            {
                Trace.Info($"The proxy url is not a well formed absolute uri string: {ProxyAddress}.");
                ProxyAddress = string.Empty;
            }

            if (!string.IsNullOrEmpty(ProxyAddress))
            {
                Trace.Info($"Config proxy at: {ProxyAddress}.");

                ProxyUsername = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY_USERNAME");
                ProxyPassword = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY_PASSWORD");

                if (!string.IsNullOrEmpty(ProxyPassword))
                {
                    var secretMasker = HostContext.GetService<ISecretMasker>();
                    secretMasker.AddValue(ProxyPassword);
                }

                if (string.IsNullOrEmpty(ProxyUsername) || string.IsNullOrEmpty(ProxyPassword))
                {
                    Trace.Info($"Config proxy use DefaultNetworkCredentials.");
                    Credentials = CredentialCache.DefaultNetworkCredentials;
                }
                else
                {
                    Trace.Info($"Config authentication proxy as: {ProxyUsername}.");
                    Credentials = new NetworkCredential(ProxyUsername, ProxyPassword);
                }

                string proxyBypassFile = IOUtil.GetProxyBypassFilePath();
                if (File.Exists(proxyBypassFile))
                {
                    Trace.Verbose($"Try read proxy bypass list from file: {proxyBypassFile}.");
                    foreach (string bypass in File.ReadAllLines(proxyBypassFile))
                    {
                        if (string.IsNullOrWhiteSpace(bypass))
                        {
                            continue;
                        }
                        else
                        {
                            Trace.Info($"Bypass proxy for: {bypass}.");
                            try
                            {
                                Regex bypassRegex = new Regex(bypass.Trim(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ECMAScript);
                                _regExBypassList.Add(bypassRegex);
                                ProxyBypassList.Add(bypass.Trim());
                            }
                            catch (Exception ex)
                            {
                                Trace.Error($"{bypass} is not a valid Regex, won't bypass proxy for {bypass}.");
                                Trace.Error(ex);
                            }
                        }
                    }
                }
            }
            else
            {
                Trace.Info($"No proxy setting found.");
            }
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
