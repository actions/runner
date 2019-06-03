using Runner.Common.Util;
using System;
using System.Linq;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Runner.Sdk;

namespace Runner.Common
{
    [ServiceLocator(Default = typeof(VstsAgentWebProxy))]
    public interface IVstsAgentWebProxy : IAgentService
    {
        string ProxyAddress { get; }
        string ProxyUsername { get; }
        string ProxyPassword { get; }
        List<string> ProxyBypassList { get; }
        IWebProxy WebProxy { get; }
    }

    public class VstsAgentWebProxy : AgentService, IVstsAgentWebProxy
    {
        private readonly List<Regex> _regExBypassList = new List<Regex>();
        private readonly List<string> _bypassList = new List<string>();
        private AgentWebProxy _agentWebProxy = new AgentWebProxy();

        public string ProxyAddress { get; private set; }
        public string ProxyUsername { get; private set; }
        public string ProxyPassword { get; private set; }
        public List<string> ProxyBypassList => _bypassList;
        public IWebProxy WebProxy => _agentWebProxy;

        public override void Initialize(IHostContext context)
        {
            base.Initialize(context);
            LoadProxySetting();
        }

        // This should only be called from config
        public void SetupProxy(string proxyAddress, string proxyUsername, string proxyPassword)
        {
            ArgUtil.NotNullOrEmpty(proxyAddress, nameof(proxyAddress));
            Trace.Info($"Update proxy setting from '{ProxyAddress ?? string.Empty}' to'{proxyAddress}'");
            ProxyAddress = proxyAddress;
            ProxyUsername = proxyUsername;
            ProxyPassword = proxyPassword;

            if (string.IsNullOrEmpty(ProxyUsername) || string.IsNullOrEmpty(ProxyPassword))
            {
                Trace.Info($"Config proxy use DefaultNetworkCredentials.");
            }
            else
            {
                Trace.Info($"Config authentication proxy as: {ProxyUsername}.");
            }

            _agentWebProxy.Update(ProxyAddress, ProxyUsername, ProxyPassword, ProxyBypassList);
        }

        // This should only be called from config
        public void SaveProxySetting()
        {
            if (!string.IsNullOrEmpty(ProxyAddress))
            {
                string proxyConfigFile = HostContext.GetConfigFile(WellKnownConfigFile.Proxy);
                IOUtil.DeleteFile(proxyConfigFile);
                Trace.Info($"Store proxy configuration to '{proxyConfigFile}' for proxy '{ProxyAddress}'");
                File.WriteAllText(proxyConfigFile, ProxyAddress);
                File.SetAttributes(proxyConfigFile, File.GetAttributes(proxyConfigFile) | FileAttributes.Hidden);

                string proxyCredFile = HostContext.GetConfigFile(WellKnownConfigFile.ProxyCredentials);
                IOUtil.DeleteFile(proxyCredFile);
                if (!string.IsNullOrEmpty(ProxyUsername) && !string.IsNullOrEmpty(ProxyPassword))
                {
                    string lookupKey = Guid.NewGuid().ToString("D").ToUpperInvariant();
                    Trace.Info($"Store proxy credential lookup key '{lookupKey}' to '{proxyCredFile}'");
                    File.WriteAllText(proxyCredFile, lookupKey);
                    File.SetAttributes(proxyCredFile, File.GetAttributes(proxyCredFile) | FileAttributes.Hidden);

                    var credStore = HostContext.GetService<IAgentCredentialStore>();
                    credStore.Write($"VSTS_AGENT_PROXY_{lookupKey}", ProxyUsername, ProxyPassword);
                }
            }
            else
            {
                Trace.Info("No proxy configuration exist.");
            }
        }

        // This should only be called from unconfig
        public void DeleteProxySetting()
        {
            string proxyCredFile = HostContext.GetConfigFile(WellKnownConfigFile.ProxyCredentials);
            if (File.Exists(proxyCredFile))
            {
                Trace.Info("Delete proxy credential from credential store.");
                string lookupKey = File.ReadAllLines(proxyCredFile).FirstOrDefault();
                if (!string.IsNullOrEmpty(lookupKey))
                {
                    var credStore = HostContext.GetService<IAgentCredentialStore>();
                    credStore.Delete($"VSTS_AGENT_PROXY_{lookupKey}");
                }

                Trace.Info($"Delete .proxycredentials file: {proxyCredFile}");
                IOUtil.DeleteFile(proxyCredFile);
            }

            string proxyBypassFile = HostContext.GetConfigFile(WellKnownConfigFile.ProxyBypass);
            if (File.Exists(proxyBypassFile))
            {
                Trace.Info($"Delete .proxybypass file: {proxyBypassFile}");
                IOUtil.DeleteFile(proxyBypassFile);
            }

            string proxyConfigFile = HostContext.GetConfigFile(WellKnownConfigFile.Proxy);
            Trace.Info($"Delete .proxy file: {proxyConfigFile}");
            IOUtil.DeleteFile(proxyConfigFile);
        }

        private void LoadProxySetting()
        {
            string proxyConfigFile = HostContext.GetConfigFile(WellKnownConfigFile.Proxy);
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

                string proxyCredFile = HostContext.GetConfigFile(WellKnownConfigFile.ProxyCredentials);
                if (File.Exists(proxyCredFile))
                {
                    string lookupKey = File.ReadAllLines(proxyCredFile).FirstOrDefault();
                    if (!string.IsNullOrEmpty(lookupKey))
                    {
                        var credStore = HostContext.GetService<IAgentCredentialStore>();
                        var proxyCred = credStore.Read($"VSTS_AGENT_PROXY_{lookupKey}");
                        ProxyUsername = proxyCred.UserName;
                        ProxyPassword = proxyCred.Password;
                    }
                }

                if (string.IsNullOrEmpty(ProxyUsername))
                {
                    ProxyUsername = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY_USERNAME");
                }

                if (string.IsNullOrEmpty(ProxyPassword))
                {
                    ProxyPassword = Environment.GetEnvironmentVariable("VSTS_HTTP_PROXY_PASSWORD");
                }

                if (!string.IsNullOrEmpty(ProxyPassword))
                {
                    HostContext.SecretMasker.AddValue(ProxyPassword);
                }

                if (string.IsNullOrEmpty(ProxyUsername) || string.IsNullOrEmpty(ProxyPassword))
                {
                    Trace.Info($"Config proxy use DefaultNetworkCredentials.");
                }
                else
                {
                    Trace.Info($"Config authentication proxy as: {ProxyUsername}.");
                }

                string proxyBypassFile = HostContext.GetConfigFile(WellKnownConfigFile.ProxyBypass);
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
                            ProxyBypassList.Add(bypass.Trim());
                        }
                    }
                }

                _agentWebProxy.Update(ProxyAddress, ProxyUsername, ProxyPassword, ProxyBypassList);
            }
            else
            {
                Trace.Info($"No proxy setting found.");
            }
        }
    }
}
