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
using Microsoft.VisualStudio.Services.Agent.Util;
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

    public class AgentCommandPluginExecutionContext : ITraceWriter
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

        public void Info(string message)
        {
            Debug(message);
        }

        public void Verbose(string message)
        {
#if DEBUG
            Debug(message);
#else
            string vstsAgentTrace = Environment.GetEnvironmentVariable("VSTSAGENT_TRACE");
            if (!string.IsNullOrEmpty(vstsAgentTrace))
            {
                Debug(message);
            }
#endif
        }

        public void Debug(string message)
        {
            if (StringUtil.ConvertToBoolean(this.Variables.GetValueOrDefault("system.debug")?.Value))
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

#if OS_LINUX || OS_OSX
            // The .NET Core 2.1 runtime switched its HTTP default from HTTP 1.1 to HTTP 2.
            // This causes problems with some versions of the Curl handler.
            // See GitHub issue https://github.com/dotnet/corefx/issues/32376
            VssClientHttpRequestSettings.Default.UseHttp11 = true;
#endif

            var certSetting = GetCertConfiguration();
            if (certSetting != null)
            {
                if (!string.IsNullOrEmpty(certSetting.ClientCertificateArchiveFile))
                {
                    VssClientHttpRequestSettings.Default.ClientCertificateManager = new AgentClientCertificateManager(certSetting.ClientCertificateArchiveFile, certSetting.ClientCertificatePassword);
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
                    VssHttpMessageHandler.DefaultWebProxy = new AgentWebProxy(proxySetting.ProxyAddress, proxySetting.ProxyUsername, proxySetting.ProxyPassword, proxySetting.ProxyBypassList);
                }
            }

            ServiceEndpoint systemConnection = this.Endpoints.FirstOrDefault(e => string.Equals(e.Name, WellKnownServiceEndpointNames.SystemVssConnection, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(systemConnection, nameof(systemConnection));
            ArgUtil.NotNull(systemConnection.Url, nameof(systemConnection.Url));

            VssCredentials credentials = VssUtil.GetVssCredential(systemConnection);
            ArgUtil.NotNull(credentials, nameof(credentials));
            return VssUtil.CreateConnection(systemConnection.Url, credentials);
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

        private AgentCertificateSettings GetCertConfiguration()
        {
            bool skipCertValidation = StringUtil.ConvertToBoolean(this.Variables.GetValueOrDefault("Agent.SkipCertValidation")?.Value);
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
                List<string> proxyBypassHosts = StringUtil.ConvertFromJson<List<string>>(this.Variables.GetValueOrDefault("Agent.ProxyBypassList")?.Value ?? "[]");
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
