using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using GitHub.DistributedTask.Logging;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common
{
    public interface IHostContext : IDisposable
    {
        StartupType StartupType { get; set; }
        CancellationToken RunnerShutdownToken { get; }
        ShutdownReason RunnerShutdownReason { get; }
        ISecretMasker SecretMasker { get; }
        List<ProductInfoHeaderValue> UserAgents { get; }
        RunnerWebProxy WebProxy { get; }
        string GetDirectory(WellKnownDirectory directory);
        string GetConfigFile(WellKnownConfigFile configFile);
        Tracing GetTrace(string name);
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
        T CreateService<T>() where T : class, IRunnerService;
        T GetService<T>() where T : class, IRunnerService;
        void SetDefaultCulture(string name);
        event EventHandler Unloading;
        void ShutdownRunner(ShutdownReason reason);
        void WritePerfCounter(string counter);
    }

    public enum StartupType
    {
        Manual,
        Service,
        AutoStartup
    }

    public sealed class HostContext : EventListener, IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>, IHostContext, IDisposable
    {
        private const int _defaultLogPageSize = 8;  //MB
        private static int _defaultLogRetentionDays = 30;
        private static int[] _vssHttpMethodEventIds = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 24 };
        private static int[] _vssHttpCredentialEventIds = new int[] { 11, 13, 14, 15, 16, 17, 18, 20, 21, 22, 27, 29 };
        private readonly ConcurrentDictionary<Type, object> _serviceInstances = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, Type> _serviceTypes = new ConcurrentDictionary<Type, Type>();
        private readonly ISecretMasker _secretMasker = new SecretMasker();
        private readonly List<ProductInfoHeaderValue> _userAgents = new List<ProductInfoHeaderValue>() { new ProductInfoHeaderValue($"GitHubActionsRunner-{BuildConstants.RunnerPackage.PackageName}", BuildConstants.RunnerPackage.Version) };
        private CancellationTokenSource _runnerShutdownTokenSource = new CancellationTokenSource();
        private object _perfLock = new object();
        private Tracing _trace;
        private Tracing _actionsHttpTrace;
        private Tracing _netcoreHttpTrace;
        private ITraceManager _traceManager;
        private AssemblyLoadContext _loadContext;
        private IDisposable _httpTraceSubscription;
        private IDisposable _diagListenerSubscription;
        private StartupType _startupType;
        private string _perfFile;
        private RunnerWebProxy _webProxy = new RunnerWebProxy();

        public event EventHandler Unloading;
        public CancellationToken RunnerShutdownToken => _runnerShutdownTokenSource.Token;
        public ShutdownReason RunnerShutdownReason { get; private set; }
        public ISecretMasker SecretMasker => _secretMasker;
        public List<ProductInfoHeaderValue> UserAgents => _userAgents;
        public RunnerWebProxy WebProxy => _webProxy;
        public HostContext(string hostType, string logFile = null)
        {
            // Validate args.
            ArgUtil.NotNullOrEmpty(hostType, nameof(hostType));

            _loadContext = AssemblyLoadContext.GetLoadContext(typeof(HostContext).GetTypeInfo().Assembly);
            _loadContext.Unloading += LoadContext_Unloading;

            this.SecretMasker.AddValueEncoder(ValueEncoders.Base64StringEscape);
            this.SecretMasker.AddValueEncoder(ValueEncoders.Base64StringEscapeShift1);
            this.SecretMasker.AddValueEncoder(ValueEncoders.Base64StringEscapeShift2);
            this.SecretMasker.AddValueEncoder(ValueEncoders.ExpressionStringEscape);
            this.SecretMasker.AddValueEncoder(ValueEncoders.JsonStringEscape);
            this.SecretMasker.AddValueEncoder(ValueEncoders.UriDataEscape);
            this.SecretMasker.AddValueEncoder(ValueEncoders.XmlDataEscape);
            this.SecretMasker.AddValueEncoder(ValueEncoders.TrimDoubleQuotes);

            // Create the trace manager.
            if (string.IsNullOrEmpty(logFile))
            {
                int logPageSize;
                string logSizeEnv = Environment.GetEnvironmentVariable($"{hostType.ToUpperInvariant()}_LOGSIZE");
                if (!string.IsNullOrEmpty(logSizeEnv) || !int.TryParse(logSizeEnv, out logPageSize))
                {
                    logPageSize = _defaultLogPageSize;
                }

                int logRetentionDays;
                string logRetentionDaysEnv = Environment.GetEnvironmentVariable($"{hostType.ToUpperInvariant()}_LOGRETENTION");
                if (!string.IsNullOrEmpty(logRetentionDaysEnv) || !int.TryParse(logRetentionDaysEnv, out logRetentionDays))
                {
                    logRetentionDays = _defaultLogRetentionDays;
                }

                // this should give us _diag folder under runner root directory
                string diagLogDirectory = Path.Combine(new DirectoryInfo(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)).Parent.FullName, Constants.Path.DiagDirectory);
                _traceManager = new TraceManager(new HostTraceListener(diagLogDirectory, hostType, logPageSize, logRetentionDays), this.SecretMasker);
            }
            else
            {
                _traceManager = new TraceManager(new HostTraceListener(logFile), this.SecretMasker);
            }

            _trace = GetTrace(nameof(HostContext));
            _actionsHttpTrace = GetTrace("GitHubActionsService");
            // Enable Http trace
            bool enableHttpTrace;
            if (bool.TryParse(Environment.GetEnvironmentVariable("GITHUB_ACTIONS_RUNNER_HTTPTRACE"), out enableHttpTrace) && enableHttpTrace)
            {
                _trace.Warning("*****************************************************************************************");
                _trace.Warning("**                                                                                     **");
                _trace.Warning("** Http trace is enabled, all your http traffic will be dumped into runner diag log.   **");
                _trace.Warning("** DO NOT share the log in public place! The trace may contains secrets in plain text. **");
                _trace.Warning("**                                                                                     **");
                _trace.Warning("*****************************************************************************************");

                _netcoreHttpTrace = GetTrace("HttpTrace");
                _diagListenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            }

            // Enable perf counter trace
            string perfCounterLocation = Environment.GetEnvironmentVariable("RUNNER_PERFLOG");
            if (!string.IsNullOrEmpty(perfCounterLocation))
            {
                try
                {
                    Directory.CreateDirectory(perfCounterLocation);
                    _perfFile = Path.Combine(perfCounterLocation, $"{hostType}.perf");
                }
                catch (Exception ex)
                {
                    _trace.Error(ex);
                }
            }

            // Check and trace proxy info
            if (!string.IsNullOrEmpty(WebProxy.HttpProxyAddress))
            {
                if (string.IsNullOrEmpty(WebProxy.HttpProxyUsername) && string.IsNullOrEmpty(WebProxy.HttpProxyPassword))
                {
                    _trace.Info($"Configuring anonymous proxy {WebProxy.HttpProxyAddress} for all HTTP requests.");
                }
                else
                {
                    // Register proxy password as secret
                    if (!string.IsNullOrEmpty(WebProxy.HttpProxyPassword))
                    {
                        this.SecretMasker.AddValue(WebProxy.HttpProxyPassword);
                    }

                    _trace.Info($"Configuring authenticated proxy {WebProxy.HttpProxyAddress} for all HTTP requests.");
                }
            }

            if (!string.IsNullOrEmpty(WebProxy.HttpsProxyAddress))
            {
                if (string.IsNullOrEmpty(WebProxy.HttpsProxyUsername) && string.IsNullOrEmpty(WebProxy.HttpsProxyPassword))
                {
                    _trace.Info($"Configuring anonymous proxy {WebProxy.HttpsProxyAddress} for all HTTPS requests.");
                }
                else
                {
                    // Register proxy password as secret
                    if (!string.IsNullOrEmpty(WebProxy.HttpsProxyPassword))
                    {
                        this.SecretMasker.AddValue(WebProxy.HttpsProxyPassword);
                    }

                    _trace.Info($"Configuring authenticated proxy {WebProxy.HttpsProxyAddress} for all HTTPS requests.");
                }
            }

            if (string.IsNullOrEmpty(WebProxy.HttpProxyAddress) && string.IsNullOrEmpty(WebProxy.HttpsProxyAddress))
            {
                _trace.Info($"No proxy settings were found based on environmental variables (http_proxy/https_proxy/HTTP_PROXY/HTTPS_PROXY)");
            }

            var credFile = GetConfigFile(WellKnownConfigFile.Credentials);
            if (File.Exists(credFile))
            {
                var credData = IOUtil.LoadObject<CredentialData>(credFile);
                if (credData != null &&
                    credData.Data.TryGetValue("clientId", out var clientId))
                {
                    _userAgents.Add(new ProductInfoHeaderValue($"RunnerId", clientId));
                }
            }
        }

        public string GetDirectory(WellKnownDirectory directory)
        {
            string path;
            switch (directory)
            {
                case WellKnownDirectory.Bin:
                    path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    break;

                case WellKnownDirectory.Diag:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        Constants.Path.DiagDirectory);
                    break;

                case WellKnownDirectory.Externals:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        Constants.Path.ExternalsDirectory);
                    break;

                case WellKnownDirectory.Root:
                    path = new DirectoryInfo(GetDirectory(WellKnownDirectory.Bin)).Parent.FullName;
                    break;

                case WellKnownDirectory.Temp:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Work),
                        Constants.Path.TempDirectory);
                    break;

                case WellKnownDirectory.Actions:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Work),
                        Constants.Path.ActionsDirectory);
                    break;

                case WellKnownDirectory.Tools:
                    // TODO: Coallesce to just check RUNNER_TOOL_CACHE when images stabilize
                    path = Environment.GetEnvironmentVariable("RUNNER_TOOL_CACHE") ?? Environment.GetEnvironmentVariable("RUNNER_TOOLSDIRECTORY") ?? Environment.GetEnvironmentVariable("AGENT_TOOLSDIRECTORY") ?? Environment.GetEnvironmentVariable(Constants.Variables.Agent.ToolsDirectory);

                    if (string.IsNullOrEmpty(path))
                    {
                        path = Path.Combine(
                            GetDirectory(WellKnownDirectory.Work),
                            Constants.Path.ToolDirectory);
                    }
                    break;

                case WellKnownDirectory.Update:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Work),
                        Constants.Path.UpdateDirectory);
                    break;

                case WellKnownDirectory.Work:
                    var configurationStore = GetService<IConfigurationStore>();
                    RunnerSettings settings = configurationStore.GetSettings();
                    ArgUtil.NotNull(settings, nameof(settings));
                    ArgUtil.NotNullOrEmpty(settings.WorkFolder, nameof(settings.WorkFolder));
                    path = Path.GetFullPath(Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        settings.WorkFolder));
                    break;

                default:
                    throw new NotSupportedException($"Unexpected well known directory: '{directory}'");
            }

            _trace.Info($"Well known directory '{directory}': '{path}'");
            return path;
        }

        public string GetConfigFile(WellKnownConfigFile configFile)
        {
            string path;
            switch (configFile)
            {
                case WellKnownConfigFile.Runner:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".runner");
                    break;

                case WellKnownConfigFile.Credentials:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".credentials");
                    break;

                case WellKnownConfigFile.MigratedCredentials:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".credentials_migrated");
                    break;

                case WellKnownConfigFile.RSACredentials:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".credentials_rsaparams");
                    break;

                case WellKnownConfigFile.Service:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".service");
                    break;

                case WellKnownConfigFile.CredentialStore:
#if OS_OSX
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".credential_store.keychain");
#else
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".credential_store");
#endif
                    break;

                case WellKnownConfigFile.Certificates:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".certificates");
                    break;

                case WellKnownConfigFile.Options:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".options");
                    break;

                case WellKnownConfigFile.SetupInfo:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        ".setup_info");
                    break;

                default:
                    throw new NotSupportedException($"Unexpected well known config file: '{configFile}'");
            }

            _trace.Info($"Well known config file '{configFile}': '{path}'");
            return path;
        }

        public Tracing GetTrace(string name)
        {
            return _traceManager[name];
        }

        public async Task Delay(TimeSpan delay, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
        }

        /// <summary>
        /// Creates a new instance of T.
        /// </summary>
        public T CreateService<T>() where T : class, IRunnerService
        {
            Type target;
            if (!_serviceTypes.TryGetValue(typeof(T), out target))
            {
                // Infer the concrete type from the ServiceLocatorAttribute.
                CustomAttributeData attribute = typeof(T)
                    .GetTypeInfo()
                    .CustomAttributes
                    .FirstOrDefault(x => x.AttributeType == typeof(ServiceLocatorAttribute));
                if (attribute != null)
                {
                    foreach (CustomAttributeNamedArgument arg in attribute.NamedArguments)
                    {
                        if (string.Equals(arg.MemberName, ServiceLocatorAttribute.DefaultPropertyName, StringComparison.Ordinal))
                        {
                            target = arg.TypedValue.Value as Type;
                        }
                    }
                }

                if (target == null)
                {
                    throw new KeyNotFoundException(string.Format(CultureInfo.InvariantCulture, "Service mapping not found for key '{0}'.", typeof(T).FullName));
                }

                _serviceTypes.TryAdd(typeof(T), target);
                target = _serviceTypes[typeof(T)];
            }

            // Create a new instance.
            T svc = Activator.CreateInstance(target) as T;
            svc.Initialize(this);
            return svc;
        }

        /// <summary>
        /// Gets or creates an instance of T.
        /// </summary>
        public T GetService<T>() where T : class, IRunnerService
        {
            // Return the cached instance if one already exists.
            object instance;
            if (_serviceInstances.TryGetValue(typeof(T), out instance))
            {
                return instance as T;
            }

            // Otherwise create a new instance and try to add it to the cache.
            _serviceInstances.TryAdd(typeof(T), CreateService<T>());

            // Return the instance from the cache.
            return _serviceInstances[typeof(T)] as T;
        }

        public void SetDefaultCulture(string name)
        {
            ArgUtil.NotNull(name, nameof(name));
            _trace.Verbose($"Setting default culture and UI culture to: '{name}'");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(name);
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(name);
        }


        public void ShutdownRunner(ShutdownReason reason)
        {
            ArgUtil.NotNull(reason, nameof(reason));
            _trace.Info($"Runner will be shutdown for {reason.ToString()}");
            RunnerShutdownReason = reason;
            _runnerShutdownTokenSource.Cancel();
        }

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public StartupType StartupType
        {
            get
            {
                return _startupType;
            }
            set
            {
                _startupType = value;
            }
        }

        public void WritePerfCounter(string counter)
        {
            if (!string.IsNullOrEmpty(_perfFile))
            {
                string normalizedCounter = counter.Replace(':', '_');
                lock (_perfLock)
                {
                    try
                    {
                        File.AppendAllLines(_perfFile, new[] { $"{normalizedCounter}:{DateTime.UtcNow.ToString("O")}" });
                    }
                    catch (Exception ex)
                    {
                        _trace.Error(ex);
                    }
                }
            }
        }

        private void Dispose(bool disposing)
        {
            // TODO: Dispose the trace listener also.
            if (disposing)
            {
                if (_loadContext != null)
                {
                    _loadContext.Unloading -= LoadContext_Unloading;
                    _loadContext = null;
                }
                _httpTraceSubscription?.Dispose();
                _diagListenerSubscription?.Dispose();
                _traceManager?.Dispose();
                _traceManager = null;

                _runnerShutdownTokenSource?.Dispose();
                _runnerShutdownTokenSource = null;

                base.Dispose();
            }
        }

        private void LoadContext_Unloading(AssemblyLoadContext obj)
        {
            if (Unloading != null)
            {
                Unloading(this, null);
            }
        }

        void IObserver<DiagnosticListener>.OnCompleted()
        {
            _netcoreHttpTrace.Info("DiagListeners finished transmitting data.");
        }

        void IObserver<DiagnosticListener>.OnError(Exception error)
        {
            _netcoreHttpTrace.Error(error);
        }

        void IObserver<DiagnosticListener>.OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "HttpHandlerDiagnosticListener" && _httpTraceSubscription == null)
            {
                _httpTraceSubscription = listener.Subscribe(this);
            }
        }

        void IObserver<KeyValuePair<string, object>>.OnCompleted()
        {
            _netcoreHttpTrace.Info("HttpHandlerDiagnosticListener finished transmitting data.");
        }

        void IObserver<KeyValuePair<string, object>>.OnError(Exception error)
        {
            _netcoreHttpTrace.Error(error);
        }

        void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value)
        {
            _netcoreHttpTrace.Info($"Trace {value.Key} event:{Environment.NewLine}{value.Value.ToString()}");
        }

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("GitHub-Actions-Http"))
            {
                EnableEvents(source, EventLevel.Verbose);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData == null)
            {
                return;
            }

            string message = eventData.Message;
            object[] payload = new object[0];
            if (eventData.Payload != null && eventData.Payload.Count > 0)
            {
                payload = eventData.Payload.ToArray();
            }

            try
            {
                if (_vssHttpMethodEventIds.Contains(eventData.EventId))
                {
                    payload[0] = Enum.Parse(typeof(VssHttpMethod), ((int)payload[0]).ToString());
                }
                else if (_vssHttpCredentialEventIds.Contains(eventData.EventId))
                {
                    payload[0] = Enum.Parse(typeof(GitHub.Services.Common.VssCredentialsType), ((int)payload[0]).ToString());
                }

                if (payload.Length > 0)
                {
                    message = String.Format(eventData.Message.Replace("%n", Environment.NewLine), payload);
                }

                switch (eventData.Level)
                {
                    case EventLevel.Critical:
                    case EventLevel.Error:
                        _actionsHttpTrace.Error(message);
                        break;
                    case EventLevel.Warning:
                        _actionsHttpTrace.Warning(message);
                        break;
                    case EventLevel.Informational:
                        _actionsHttpTrace.Info(message);
                        break;
                    default:
                        _actionsHttpTrace.Verbose(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _actionsHttpTrace.Error(ex);
                _actionsHttpTrace.Info(eventData.Message);
                _actionsHttpTrace.Info(string.Join(", ", eventData.Payload?.ToArray() ?? new string[0]));
            }
        }

        // Copied from pipelines server code base, used for EventData translation.
        internal enum VssHttpMethod
        {
            UNKNOWN,
            DELETE,
            HEAD,
            GET,
            OPTIONS,
            PATCH,
            POST,
            PUT,
        }
    }

    public static class HostContextExtension
    {
        public static HttpClientHandler CreateHttpClientHandler(this IHostContext context)
        {
            var handlerFactory = context.GetService<IHttpClientHandlerFactory>();
            return handlerFactory.CreateClientHandler(context.WebProxy);
        }
    }

    public enum ShutdownReason
    {
        UserCancelled = 0,
        OperatingSystemShutdown = 1,
    }
}
