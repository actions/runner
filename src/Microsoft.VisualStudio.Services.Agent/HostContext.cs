using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.Diagnostics.Tracing;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IHostContext : IDisposable
    {
        RunMode RunMode { get; set; }
        string GetDirectory(WellKnownDirectory directory);
        Tracing GetTrace(string name);
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
        T CreateService<T>() where T : class, IAgentService;
        T GetService<T>() where T : class, IAgentService;
        void SetDefaultCulture(string name);
        event EventHandler Unloading;
        StartupType StartupType { get; set; }
        CancellationToken AgentShutdownToken { get; }
        ShutdownReason AgentShutdownReason { get; }
        void ShutdownAgent(ShutdownReason reason);
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
        private CancellationTokenSource _agentShutdownTokenSource = new CancellationTokenSource();
        private RunMode _runMode = RunMode.Normal;
        private Tracing _trace;
        private Tracing _vssTrace;
        private Tracing _httpTrace;
        private ITraceManager _traceManager;
        private AssemblyLoadContext _loadContext;
        private IDisposable _httpTraceSubscription;
        private IDisposable _diagListenerSubscription;

        private StartupType _startupType;

        public event EventHandler Unloading;
        public CancellationToken AgentShutdownToken => _agentShutdownTokenSource.Token;
        public ShutdownReason AgentShutdownReason { get; private set; }
        public HostContext(string hostType, string logFile = null)
        {
            // Validate args.
            ArgUtil.NotNullOrEmpty(hostType, nameof(hostType));

            _loadContext = AssemblyLoadContext.GetLoadContext(typeof(HostContext).GetTypeInfo().Assembly);
            _loadContext.Unloading += LoadContext_Unloading;

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

                _traceManager = new TraceManager(new HostTraceListener(hostType, logPageSize, logRetentionDays), GetService<ISecretMasker>());
            }
            else
            {
                _traceManager = new TraceManager(new HostTraceListener(logFile), GetService<ISecretMasker>());
            }

            _trace = GetTrace(nameof(HostContext));
            _vssTrace = GetTrace(nameof(VisualStudio) + nameof(VisualStudio.Services));  // VisualStudioService

            // Enable Http trace
            bool enableHttpTrace;
            if (bool.TryParse(Environment.GetEnvironmentVariable("VSTS_AGENT_HTTPTRACE"), out enableHttpTrace) && enableHttpTrace)
            {
                _trace.Warning("*****************************************************************************************");
                _trace.Warning("**                                                                                     **");
                _trace.Warning("** Http trace is enabled, all your http traffic will be dumped into agent diag log.    **");
                _trace.Warning("** DO NOT share the log in public place! The trace may contains secrets in plain text. **");
                _trace.Warning("**                                                                                     **");
                _trace.Warning("*****************************************************************************************");

                _httpTrace = GetTrace("HttpTrace");
                _diagListenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            }
        }

        public RunMode RunMode
        {
            get
            {
                return _runMode;
            }

            set
            {
                _trace.Info($"Set run mode: {value}");
                _runMode = value;
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

                case WellKnownDirectory.LegacyPSHost:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Externals),
                        Constants.Path.LegacyPSHostDirectory);
                    break;

                case WellKnownDirectory.Root:
                    path = new DirectoryInfo(GetDirectory(WellKnownDirectory.Bin)).Parent.FullName;
                    break;

                case WellKnownDirectory.ServerOM:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Externals),
                        Constants.Path.ServerOMDirectory);
                    break;

                case WellKnownDirectory.Tee:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Externals),
                        Constants.Path.TeeDirectory);
                    break;

                case WellKnownDirectory.Tasks:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Work),
                        Constants.Path.TasksDirectory);
                    break;

                case WellKnownDirectory.Update:
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Work),
                        Constants.Path.UpdateDirectory);
                    break;

                case WellKnownDirectory.Work:
                    var configurationStore = GetService<IConfigurationStore>();
                    AgentSettings settings = configurationStore.GetSettings();
                    ArgUtil.NotNull(settings, nameof(settings));
                    ArgUtil.NotNullOrEmpty(settings.WorkFolder, nameof(settings.WorkFolder));
                    path = Path.Combine(
                        GetDirectory(WellKnownDirectory.Root),
                        settings.WorkFolder);
                    break;

                default:
                    throw new NotSupportedException($"Unexpected well known directory: '{directory}'");
            }

            _trace.Info($"Well known directory '{directory}': '{path}'");
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
        public T CreateService<T>() where T : class, IAgentService
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
        public T GetService<T>() where T : class, IAgentService
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


        public void ShutdownAgent(ShutdownReason reason)
        {
            ArgUtil.NotNull(reason, nameof(reason));
            _trace.Info($"Agent will be shutdown for {reason.ToString()}");
            AgentShutdownReason = reason;
            _agentShutdownTokenSource.Cancel();
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

                _agentShutdownTokenSource?.Dispose();
                _agentShutdownTokenSource = null;

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
            _httpTrace.Info("DiagListeners finished transmitting data.");
        }

        void IObserver<DiagnosticListener>.OnError(Exception error)
        {
            _httpTrace.Error(error);
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
            _httpTrace.Info("HttpHandlerDiagnosticListener finished transmitting data.");
        }

        void IObserver<KeyValuePair<string, object>>.OnError(Exception error)
        {
            _httpTrace.Error(error);
        }

        void IObserver<KeyValuePair<string, object>>.OnNext(KeyValuePair<string, object> value)
        {
            _httpTrace.Info($"Trace {value.Key} event:{Environment.NewLine}{value.Value.ToString()}");
        }

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name.Equals("Microsoft-VSS-Http"))
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
                    payload[0] = Enum.Parse(typeof(VisualStudio.Services.Common.VssCredentialsType), ((int)payload[0]).ToString());
                }

                if (payload.Length > 0)
                {
                    message = String.Format(eventData.Message.Replace("%n", Environment.NewLine), payload);
                }

                switch (eventData.Level)
                {
                    case EventLevel.Critical:
                    case EventLevel.Error:
                        _vssTrace.Error(message);
                        break;
                    case EventLevel.Warning:
                        _vssTrace.Warning(message);
                        break;
                    case EventLevel.Informational:
                        _vssTrace.Info(message);
                        break;
                    default:
                        _vssTrace.Verbose(message);
                        break;
                }
            }
            catch (Exception ex)
            {
                _vssTrace.Error(ex);
                _vssTrace.Info(eventData.Message);
                _vssTrace.Info(string.Join(", ", eventData.Payload?.ToArray() ?? new string[0]));
            }
        }

        // Copied from VSTS code base, used for EventData translation.
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
            HttpClientHandler clientHandler = new HttpClientHandler();
            var agentWebProxy = context.GetService<IVstsAgentWebProxy>();
            clientHandler.Proxy = agentWebProxy;
            return clientHandler;
        }
    }

    public enum ShutdownReason
    {
        UserCancelled = 0,
        OperatingSystemShutdown = 1,
    }
}