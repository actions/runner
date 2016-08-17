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
        String ProxyUrl { get; }
        void ApplyProxySettings();
    }

    public class ProxyConfiguration : AgentService, IProxyConfiguration
    {
        private bool _proxySettingsApplied = false;
        private ICredentials _proxyCredential = null;

        public String ProxyUrl { get; private set; }
        public HttpClientHandler HttpClientHandlerWithProxySetting
        {
            get
            {
                HttpClientHandler clientHandler = new HttpClientHandler();

                // apply proxy setting to the new HttpClientHandler instance.
                if (_proxySettingsApplied && !string.IsNullOrEmpty(ProxyUrl))
                {
                    clientHandler.UseProxy = true;
                    clientHandler.Proxy = new WebProxy(new Uri(ProxyUrl))
                    {
                        Credentials = _proxyCredential
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
                return;
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

                string username = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY_USERNAME");
                string password = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY_PASSWORD");

                if (!string.IsNullOrEmpty(password))
                {
                    var secretMasker = HostContext.GetService<ISecretMasker>();
                    secretMasker.AddValue(password);
                }

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    Trace.Info($"Config proxy use DefaultNetworkCredentials.");
                    _proxyCredential = CredentialCache.DefaultNetworkCredentials;
                }
                else
                {
                    Trace.Info($"Config authentication proxy as: {username}.");
                    _proxyCredential = new NetworkCredential(username, password);
                }

                VssHttpMessageHandler.DefaultWebProxy = new WebProxy(new Uri(ProxyUrl))
                {
                    Credentials = _proxyCredential
                };

                _proxySettingsApplied = true;
            }
            else
            {
                Trace.Info($"No proxy setting found.");
            }
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
