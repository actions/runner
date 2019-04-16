using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Agent.Sdk
{
    public interface IAgentTaskPlugin
    {
        Guid Id { get; }
        string Version { get; }
        string Stage { get; }
        Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token);
    }

    public class AgentTaskPluginExecutionContext : ITraceWriter
    {
        private VssConnection _connection;
        private readonly object _stdoutLock = new object();
        private readonly ITraceWriter _trace; // for unit tests

        public AgentTaskPluginExecutionContext()
            : this(null)
        { }

        public AgentTaskPluginExecutionContext(ITraceWriter trace)
        {
            _trace = trace;
            this.Endpoints = new List<ServiceEndpoint>();
            this.Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Repositories = new List<Pipelines.RepositoryResource>();
            this.TaskVariables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
            this.Variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
        }

        public List<ServiceEndpoint> Endpoints { get; set; }
        public List<Pipelines.RepositoryResource> Repositories { get; set; }
        public Dictionary<string, VariableValue> Variables { get; set; }
        public Dictionary<string, VariableValue> TaskVariables { get; set; }
        public Dictionary<string, string> Inputs { get; set; }

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
        public string GetInput(string name, bool required = false)
        {
            string value = null;
            if (this.Inputs.ContainsKey(name))
            {
                value = this.Inputs[name];
            }

            if (string.IsNullOrEmpty(value) && required)
            {
                throw new ArgumentNullException(name);
            }

            return value;
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

        public void Error(string message)
        {
            Output($"##vso[task.logissue type=error;]{Escape(message)}");
            Output($"##vso[task.complete result=Failed;]");
        }

        public void Debug(string message)
        {
            Output($"##vso[task.debug]{Escape(message)}");
        }

        public void Warning(string message)
        {
            Output($"##vso[task.logissue type=warning;]{Escape(message)}");
        }

        public void PublishTelemetry(string area, string feature, Dictionary<string, string> properties)
        {      
            string propertiesAsJson = StringUtil.ConvertToJson(properties, Formatting.None);

            Output($"##vso[telemetry.publish area={area};feature={feature}]{Escape(propertiesAsJson)}");
        }           

        public void Output(string message)
        {
            lock (_stdoutLock)
            {
                if (_trace == null)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    _trace.Info(message);
                }
            }
        }

        public void PrependPath(string directory)
        {
            PathUtil.PrependPath(directory);
            Output($"##vso[task.prependpath]{Escape(directory)}");
        }

        public void Progress(int progress, string operation)
        {
            if (progress < 0 || progress > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(progress));
            }

            Output($"##vso[task.setprogress value={progress}]{Escape(operation)}");
        }

        public void SetSecret(string secret)
        {
            Output($"##vso[task.setsecret]{Escape(secret)}");
        }

        public void SetVariable(string variable, string value, bool isSecret = false)
        {
            this.Variables[variable] = new VariableValue(value, isSecret);
            Output($"##vso[task.setvariable variable={Escape(variable)};issecret={isSecret.ToString()};]{Escape(value)}");
        }

        public void SetTaskVariable(string variable, string value, bool isSecret = false)
        {
            this.TaskVariables[variable] = new VariableValue(value, isSecret);
            Output($"##vso[task.settaskvariable variable={Escape(variable)};issecret={isSecret.ToString()};]{Escape(value)}");
        }

        public void Command(string command)
        {
            Output($"##[command]{Escape(command)}");
        }

        public void UpdateRepositoryPath(string alias, string path)
        {
            Output($"##vso[plugininternal.updaterepositorypath alias={Escape(alias)};]{path}");
        }

        public AgentCertificateSettings GetCertConfiguration()
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

                    certConfig.VssClientCertificateManager = new AgentClientCertificateManager(clientCertArchive, clientCertPassword);
                }

                return certConfig;
            }
            else
            {
                return null;
            }
        }

        public AgentWebProxySettings GetProxyConfiguration()
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
                    WebProxy = new AgentWebProxy(proxyUrl, proxyUsername, proxyPassword, proxyBypassHosts)
                };
            }
            else
            {
                return null;
            }
        }

        private string Escape(string input)
        {
            foreach (var mapping in _commandEscapeMappings)
            {
                input = input.Replace(mapping.Key, mapping.Value);
            }

            return input;
        }

        private Dictionary<string, string> _commandEscapeMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {
                ";", "%3B"
            },
            {
                "\r", "%0D"
            },
            {
                "\n", "%0A"
            },
            {
                "]", "%5D"
            },
        };
    }
}
