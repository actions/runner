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
    public interface IHostContext
    {
        CancellationToken CancellationToken { get; }
        TraceSource GetTrace(string name);
        Task Delay(TimeSpan delay);
        T CreateService<T>() where T : class, IAgentService;
        T GetService<T>() where T : class, IAgentService;
    }

    public sealed class HostContext : IHostContext, IDisposable
    {
        public HostContext(string hostType)
        {
            if (string.IsNullOrEmpty(hostType))
            {
                throw new ArgumentNullException(nameof(hostType));
            }
            
            _hostType = hostType;

            // Create the trace manager.
            string fileName = String.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.log", _hostType, DateTime.UtcNow);
            var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
            var logPath = new DirectoryInfo(currentAssemblyLocation).Parent.FullName.ToString();
            Stream logFile = File.Create(Path.Combine(logPath, fileName));
            TextWriterTraceListener traceListener = new TextWriterTraceListener(logFile);
            _traceManager = new TraceManager(traceListener);
        }
       
        public CancellationToken CancellationToken
        {
            get
            {
                return _cancellationTokenSource.Token;
            }
        }

        //TODO: hide somehow this variable
        public CancellationTokenSource CancellationTokenSource
        {
            get
            {
                return _cancellationTokenSource;
            }
        }

        public TraceSource GetTrace(string name) 
        {
            return _traceManager[name];
        }

        public async Task Delay(TimeSpan delay)
        {
            await Task.Delay(delay, CancellationToken);
        }

        /// <summary>
        /// Creates a new instance of T.
        /// </summary>
        public T CreateService<T>() where T : class, IAgentService
        {
            Type target;
            if (!_serviceTypes.TryGetValue(typeof(T), out target))
            {
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

            var svc = Activator.CreateInstance(target) as T;
            svc.Initialize(this);
            return svc;
        }

        /// <summary>
        /// Gets or creates an instance of T.
        /// </summary>
        public T GetService<T>() where T : class, IAgentService
        {
            object instance;
            if (_serviceInstances.TryGetValue(typeof(T), out instance))
            {
                return instance as T;
            }

            _serviceInstances.TryAdd(typeof(T), CreateService<T>());
            return _serviceInstances[typeof(T)] as T;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _traceManager?.Dispose();
                _traceManager = null;
            }
        }

        private readonly ConcurrentDictionary<Type, object> _serviceInstances = new ConcurrentDictionary<Type, object>();
        private readonly ConcurrentDictionary<Type, Type> _serviceTypes = new ConcurrentDictionary<Type, Type>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private ITraceManager _traceManager;
        private string _hostType;
   }
}