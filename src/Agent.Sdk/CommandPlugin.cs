using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.OAuth;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Agent.Sdk
{
    public interface IAgentCommandPlugin
    {
        String Area { get; }
        String Event { get; }
        String DisplayName { get; }
        Task ProcessCommandAsync(AgentCommandPluginExecutionContext executionContext, CancellationToken token);
    }

    public class AgentCommandPluginExecutionContext
    {
        private VssConnection _connection;
        private readonly object _stdoutLock = new object();
        private readonly object _stderrLock = new object();

        public AgentCommandPluginExecutionContext()
        {
            this.Endpoints = new List<ServiceEndpoint>();
            this.Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);

#if OS_WINDOWS
            this.ContainerPathMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
#else
            this.ContainerPathMappings = new Dictionary<string, string>();
#endif
        }

        public string Data { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public List<ServiceEndpoint> Endpoints { get; set; }
        public Dictionary<string, VariableValue> Variables { get; set; }
        public Dictionary<string, string> ContainerPathMappings { get; set; }

        [JsonIgnore]
        public VssConnection VssConnection
        {
            get
            {
                if (_connection == null)
                {
                    _connection = InitializeVssConnection();
                }
                return _connection;
            }
        }

        public void Debug(string message)
        {
            if (PluginUtil.ConvertToBoolean(this.Variables.GetValueOrDefault("system.debug")?.Value))
            {
                Output($"##[debug]{message}");
            }
        }

        public void Output(string message)
        {
            lock (_stdoutLock)
            {
                Console.Out.WriteLine(message);
            }
        }

        public void Error(string message)
        {
            lock (_stderrLock)
            {
                Console.Error.WriteLine(message);
            }
        }

        public VssConnection InitializeVssConnection()
        {
            var headerValues = new List<ProductInfoHeaderValue>();
            headerValues.Add(new ProductInfoHeaderValue($"VstsAgentCore-Plugin", Variables.GetValueOrDefault("agent.version")?.Value ?? "Unknown"));
            headerValues.Add(new ProductInfoHeaderValue($"({RuntimeInformation.OSDescription.Trim()})"));

            if (VssClientHttpRequestSettings.Default.UserAgent != null && VssClientHttpRequestSettings.Default.UserAgent.Count > 0)
            {
                headerValues.AddRange(VssClientHttpRequestSettings.Default.UserAgent);
            }

            VssClientHttpRequestSettings.Default.UserAgent = headerValues;

            var certSetting = GetCertConfiguration();
            if (certSetting != null)
            {
                if (!string.IsNullOrEmpty(certSetting.ClientCertificateArchiveFile))
                {
                    VssClientHttpRequestSettings.Default.ClientCertificateManager = new CommandPluginClientCertificateManager(certSetting.ClientCertificateArchiveFile, certSetting.ClientCertificatePassword);
                }

                if (certSetting.SkipServerCertificateValidation)
                {
                    VssClientHttpRequestSettings.Default.ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
            }

            var proxySetting = GetProxyConfiguration();
            if (proxySetting != null)
            {
                if (!string.IsNullOrEmpty(proxySetting.ProxyAddress))
                {
                    VssHttpMessageHandler.DefaultWebProxy = new CommandPluginWebProxy(proxySetting.ProxyAddress, proxySetting.ProxyUsername, proxySetting.ProxyPassword, proxySetting.ProxyBypassList);
                }
            }

            ServiceEndpoint systemConnection = this.Endpoints.FirstOrDefault(e => string.Equals(e.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            PluginUtil.NotNull(systemConnection, nameof(systemConnection));
            PluginUtil.NotNull(systemConnection.Url, nameof(systemConnection.Url));

            VssCredentials credentials = PluginUtil.GetVssCredential(systemConnection);
            PluginUtil.NotNull(credentials, nameof(credentials));
            return PluginUtil.CreateConnection(systemConnection.Url, credentials);
        }

        public string TranslateContainerPathToHostPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var mapping in this.ContainerPathMappings)
                {
#if OS_WINDOWS
                    if (string.Equals(path, mapping.Value, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Key;
                    }

                    if (path.StartsWith(mapping.Value + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                        path.StartsWith(mapping.Value + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.Key + path.Remove(0, mapping.Value.Length);
                    }
#else
                    if (string.Equals(path, mapping.Value))
                    {
                        return mapping.Key;
                    }

                    if (path.StartsWith(mapping.Value + Path.DirectorySeparatorChar))
                    {
                        return mapping.Key + path.Remove(0, mapping.Value.Length);
                    }
#endif
                }
            }

            return path;
        }

        private sealed class CommandPluginWebProxy : IWebProxy
        {
            private string _proxyAddress;
            private readonly List<Regex> _regExBypassList = new List<Regex>();

            public ICredentials Credentials { get; set; }

            public CommandPluginWebProxy(string proxyAddress, string proxyUsername, string proxyPassword, List<string> proxyBypassList)
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

        private class CommandPluginClientCertificateManager : IVssClientCertificateManager
        {
            private readonly X509Certificate2Collection _clientCertificates = new X509Certificate2Collection();
            public X509Certificate2Collection ClientCertificates => _clientCertificates;
            public CommandPluginClientCertificateManager(string clientCertificateArchiveFile, string clientCertificatePassword)
            {
                if (!string.IsNullOrEmpty(clientCertificateArchiveFile))
                {
                    _clientCertificates.Add(new X509Certificate2(clientCertificateArchiveFile, clientCertificatePassword));
                }
            }
        }

        private AgentCertificateSettings GetCertConfiguration()
        {
            bool skipCertValidation = PluginUtil.ConvertToBoolean(this.Variables.GetValueOrDefault("Agent.SkipCertValidation")?.Value);
            string caFile = this.Variables.GetValueOrDefault("Agent.CAInfo")?.Value;
            string clientCertFile = this.Variables.GetValueOrDefault("Agent.ClientCert")?.Value;

            if (!string.IsNullOrEmpty(caFile) || !string.IsNullOrEmpty(clientCertFile) || skipCertValidation)
            {
                var certConfig = new AgentCertificateSettings();
                certConfig.SkipServerCertificateValidation = skipCertValidation;
                certConfig.CACertificateFile = caFile;

                if (!string.IsNullOrEmpty(clientCertFile))
                {
                    certConfig.ClientCertificateFile = clientCertFile;
                    string clientCertKey = this.Variables.GetValueOrDefault("Agent.ClientCertKey")?.Value;
                    string clientCertArchive = this.Variables.GetValueOrDefault("Agent.ClientCertArchive")?.Value;
                    string clientCertPassword = this.Variables.GetValueOrDefault("Agent.ClientCertPassword")?.Value;

                    certConfig.ClientCertificatePrivateKeyFile = clientCertKey;
                    certConfig.ClientCertificateArchiveFile = clientCertArchive;
                    certConfig.ClientCertificatePassword = clientCertPassword;
                }

                return certConfig;
            }
            else
            {
                return null;
            }
        }

        private AgentWebProxySettings GetProxyConfiguration()
        {
            string proxyUrl = this.Variables.GetValueOrDefault("Agent.ProxyUrl")?.Value;
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                string proxyUsername = this.Variables.GetValueOrDefault("Agent.ProxyUsername")?.Value;
                string proxyPassword = this.Variables.GetValueOrDefault("Agent.ProxyPassword")?.Value;
                List<string> proxyBypassHosts = PluginUtil.ConvertFromJson<List<string>>(this.Variables.GetValueOrDefault("Agent.ProxyBypassList")?.Value ?? "[]");
                return new AgentWebProxySettings()
                {
                    ProxyAddress = proxyUrl,
                    ProxyUsername = proxyUsername,
                    ProxyPassword = proxyPassword,
                    ProxyBypassList = proxyBypassHosts,
                };
            }
            else
            {
                return null;
            }
        }
    }
}
