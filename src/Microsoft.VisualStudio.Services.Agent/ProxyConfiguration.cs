using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ProxyConfiguration))]
    public interface IProxyConfiguration : IAgentService
    {
        HttpClientHandler HttpClientHandlerWithProxySetting { get; }
        string ProxyUrl { get; }
        string ProxyUsername { get; }
        string ProxyPassword { get; }
        void ApplyProxySettings();
    }

    public class ProxyConfiguration : AgentService, IProxyConfiguration
    {
        private bool _proxySettingsApplied = false;

        public string ProxyUrl { get; private set; }
        public string ProxyUsername { get; private set; }
        public string ProxyPassword { get; private set; }

        public HttpClientHandler HttpClientHandlerWithProxySetting
        {
            get
            {
                HttpClientHandler clientHandler = new HttpClientHandler();

                // apply proxy setting to the new HttpClientHandler instance.
                if (_proxySettingsApplied && !string.IsNullOrEmpty(ProxyUrl))
                {
                    clientHandler.UseProxy = true;
                    ICredentials proxyCredential = null;
                    if (string.IsNullOrEmpty(ProxyUsername) || string.IsNullOrEmpty(ProxyPassword))
                    {
                        proxyCredential = CredentialCache.DefaultNetworkCredentials;
                    }
                    else
                    {
                        proxyCredential = new NetworkCredential(ProxyUsername, ProxyPassword);
                    }

                    clientHandler.Proxy = new WebProxy(new Uri(ProxyUrl))
                    {
                        Credentials = proxyCredential
                    };
                }

                return clientHandler;
            }
        }

        public void ApplyProxySettings()
        {
            Trace.Entering();
            if (_proxySettingsApplied)
            {
                // agent and worker process should apply proxy setting upfront only once. 
                throw new InvalidOperationException(nameof(ApplyProxySettings));
            }

            string proxyConfigFile = IOUtil.GetProxyConfigFilePath();
            if (File.Exists(proxyConfigFile))
            {
                // we expect the first line of the file is the proxy url
                Trace.Verbose($"Try read proxy setting from file: {proxyConfigFile}.");
                ProxyUrl = File.ReadLines(proxyConfigFile).FirstOrDefault() ?? string.Empty;
                ProxyUrl = ProxyUrl.Trim();
                Trace.Verbose($"{ProxyUrl}");
            }

            if (string.IsNullOrEmpty(ProxyUrl))
            {
                Trace.Verbose("Try read proxy setting from environment variable: 'VSTS_HTTP_PROXY'.");
                ProxyUrl = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY") ?? string.Empty;
                ProxyUrl = ProxyUrl.Trim();
                Trace.Verbose($"{ProxyUrl}");
            }

            if (!string.IsNullOrEmpty(ProxyUrl) && !Uri.IsWellFormedUriString(ProxyUrl, UriKind.Absolute))
            {
                Trace.Info($"The proxy url is not a well formed absolute uri string: {ProxyUrl}.");
                ProxyUrl = string.Empty;
            }

            if (!string.IsNullOrEmpty(ProxyUrl))
            {
                Trace.Info($"Config proxy at: {ProxyUrl}.");

                ProxyUsername = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY_USERNAME");
                ProxyPassword = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY_PASSWORD");

                if (!string.IsNullOrEmpty(ProxyPassword))
                {
                    var secretMasker = HostContext.GetService<ISecretMasker>();
                    secretMasker.AddValue(ProxyPassword);
                }

                ICredentials proxyCredential = null;
                if (string.IsNullOrEmpty(ProxyUsername) || string.IsNullOrEmpty(ProxyPassword))
                {
                    Trace.Info($"Config proxy use DefaultNetworkCredentials.");
                    proxyCredential = CredentialCache.DefaultNetworkCredentials;
                }
                else
                {
                    Trace.Info($"Config authentication proxy as: {ProxyUsername}.");
                    proxyCredential = new NetworkCredential(ProxyUsername, ProxyPassword);
                }

                VssHttpMessageHandler.DefaultWebProxy = new WebProxy(new Uri(ProxyUrl))
                {
                    Credentials = proxyCredential
                };
            }
            else
            {
                Trace.Info($"No proxy setting found.");
            }

            _proxySettingsApplied = true;
        }
    }

    public class WebProxy : IWebProxy
    {
        public WebProxy(Uri proxyAddress)
        {
            if (proxyAddress == null)
            {
                throw new ArgumentNullException(nameof(proxyAddress));
            }

            ProxyAddress = proxyAddress;
        }

        public Uri ProxyAddress { get; private set; }

        public ICredentials Credentials { get; set; }

        public Uri GetProxy(Uri destination) => ProxyAddress;

        public bool IsBypassed(Uri uri)
        {
            return uri.IsLoopback;
        }
    }
}
