using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IHostContext : IDisposable
    {
        Tracing GetTrace(string name);
        Task Delay(TimeSpan delay, CancellationToken cancellationToken);
        T CreateService<T>() where T : class, IAgentService;
        T GetService<T>() where T : class, IAgentService;
        void SetDefaultCulture(string name);
    }

    public sealed class HostContext : IHostContext, IDisposable
    {
        private const int _defaultLogPageSize = 8;  //MB
        private static int _defaultLogRetentionDays = 30;
        private readonly ConcurrentDictionary<Type, object> _serviceInstances = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, Type> _serviceTypes = new ConcurrentDictionary<Type, Type>();
        private Tracing _trace;
        private ITraceManager _traceManager;

        public HostContext(string hostType, string logFile = null)
        {
            // Validate args.
            ArgUtil.NotNullOrEmpty(hostType, nameof(hostType));

            // Create the trace manager.
            if (string.IsNullOrEmpty(logFile))
            {
                int logPageSize;
                string logSizeEnv = Environment.GetEnvironmentVariable($"{hostType.ToLower()}.logsize");
                if (!string.IsNullOrEmpty(logSizeEnv) || !int.TryParse(logSizeEnv, out logPageSize))
                {
                    logPageSize = _defaultLogPageSize;
                }

                int logRetentionDays;
                string logRetentionDaysEnv = Environment.GetEnvironmentVariable($"{hostType.ToLower()}.logretention");
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            // TODO: Dispose the trace listener also.
            if (disposing)
            {
                _traceManager?.Dispose();
                _traceManager = null;
            }
        }
    }
}