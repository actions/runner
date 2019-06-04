using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Services.Common;
using GitHub.Services.WebApi;
using Newtonsoft.Json;
using Pipelines = GitHub.DistributedTask.Pipelines;

namespace GitHub.Runner.Sdk
{
    public interface IRunnerActionPlugin
    {
        Task RunAsync(RunnerActionPluginExecutionContext executionContext, CancellationToken token);
    }

    public class RunnerActionPluginExecutionContext : ITraceWriter
    {
        private VssConnection _connection;
        private readonly object _stdoutLock = new object();
        private readonly ITraceWriter _trace; // for unit tests

        public RunnerActionPluginExecutionContext()
            : this(null)
        { }

        public RunnerActionPluginExecutionContext(ITraceWriter trace)
        {
            _trace = trace;
            this.Endpoints = new List<ServiceEndpoint>();
            this.Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.Repositories = new List<Pipelines.RepositoryResource>();
            this.TaskVariables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
            this.Variables = new Dictionary<string, VariableValue>(StringComparer.OrdinalIgnoreCase);
            this.Context = new Dictionary<string, PipelineContextData>(StringComparer.OrdinalIgnoreCase);
        }

        public List<ServiceEndpoint> Endpoints { get; set; }
        public List<Pipelines.RepositoryResource> Repositories { get; set; }
        public Dictionary<string, VariableValue> Variables { get; set; }
        public Dictionary<string, VariableValue> TaskVariables { get; set; }
        public Dictionary<string, string> Inputs { get; set; }
        public Dictionary<String, PipelineContextData> Context { get; set; }

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
            headerValues.Add(new ProductInfoHeaderValue($"GitHubActionsRunner-Plugin", BuildConstants.RunnerPackage.Version));
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
                    VssClientHttpRequestSettings.Default.ClientCertificateManager = new RunnerClientCertificateManager(certSetting.ClientCertificateArchiveFile, certSetting.ClientCertificatePassword);
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
                    VssHttpMessageHandler.DefaultWebProxy = new RunnerWebProxyCore(proxySetting.ProxyAddress, proxySetting.ProxyUsername, proxySetting.ProxyPassword, proxySetting.ProxyBypassList);
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

            Debug($"Input '{name}': '{value ?? string.Empty}'");

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
            string vstsAgentTrace = Environment.GetEnvironmentVariable("system.debug");
            if (!string.IsNullOrEmpty(vstsAgentTrace))
            {
                Debug(message);
            }
#endif
        }

        public void Error(string message)
        {
            Output($"##[error]{Escape(message)}");
        }

        public void Debug(string message)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("system.debug")))
            {
                Output($"##[debug]{Escape(message)}");
            }
        }

        public void Warning(string message)
        {
            Output($"##[warning]{Escape(message)}");
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

        public void SetSecret(string secret)
        {
            //Output($"##[set-secret]{Escape(secret)}");
        }

        public void Command(string command)
        {
            Output($"##[command]{Escape(command)}");
        }

        public void UpdateSelfRepositoryPath(string path)
        {
            Output($"##[internal-set-self-path]{path}");
        }

        public String GetRunnerInfo(string infoName)
        {
            this.Context.TryGetValue("runner", out var context);
            var runnerContext = context as DictionaryContextData;
            ArgUtil.NotNull(runnerContext, nameof(runnerContext));
            if(runnerContext.TryGetValue(infoName, out var info))
            {
                return info as StringContextData;
            }
            else
            {
                return null;
            }
        }

        public RunnerCertificateSettings GetCertConfiguration()
        {
            bool skipCertValidation = StringUtil.ConvertToBoolean(GetRunnerInfo("SkipCertValidation"));
            string caFile = GetRunnerInfo("CAInfo");
            string clientCertFile = GetRunnerInfo("ClientCert");

            if (!string.IsNullOrEmpty(caFile) || !string.IsNullOrEmpty(clientCertFile) || skipCertValidation)
            {
                var certConfig = new RunnerCertificateSettings();
                certConfig.SkipServerCertificateValidation = skipCertValidation;
                certConfig.CACertificateFile = caFile;

                if (!string.IsNullOrEmpty(clientCertFile))
                {
                    certConfig.ClientCertificateFile = clientCertFile;
                    string clientCertKey = GetRunnerInfo("ClientCertKey");
                    string clientCertArchive = GetRunnerInfo("ClientCertArchive");
                    string clientCertPassword = GetRunnerInfo("ClientCertPassword");

                    certConfig.ClientCertificatePrivateKeyFile = clientCertKey;
                    certConfig.ClientCertificateArchiveFile = clientCertArchive;
                    certConfig.ClientCertificatePassword = clientCertPassword;

                    certConfig.VssClientCertificateManager = new RunnerClientCertificateManager(clientCertArchive, clientCertPassword);
                }

                return certConfig;
            }
            else
            {
                return null;
            }
        }

        public RunnerWebProxySettings GetProxyConfiguration()
        {
            string proxyUrl = GetRunnerInfo("ProxyUrl");
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                string proxyUsername = GetRunnerInfo("ProxyUsername");
                string proxyPassword = GetRunnerInfo("ProxyPassword");
                List<string> proxyBypassHosts = StringUtil.ConvertFromJson<List<string>>(GetRunnerInfo("ProxyBypassList") ?? "[]");
                return new RunnerWebProxySettings()
                {
                    ProxyAddress = proxyUrl,
                    ProxyUsername = proxyUsername,
                    ProxyPassword = proxyPassword,
                    ProxyBypassList = proxyBypassHosts,
                    WebProxy = new RunnerWebProxyCore(proxyUrl, proxyUsername, proxyPassword, proxyBypassHosts)
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
