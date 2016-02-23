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
        T GetService<T>() where T : class, IAgentService;
    }
    
    public sealed class HostContext : IHostContext, IDisposable
    {
        private readonly ConcurrentDictionary<Type, Type> serviceMappings = new ConcurrentDictionary<Type, Type>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private ITraceManager _traceManager;
        private String _hostType;

        public HostContext(String hostType)
        {
            if(String.IsNullOrEmpty(hostType))
            {
                throw new ArgumentNullException(nameof(hostType));
            }
            
            _hostType = hostType;
        }
       
        public CancellationToken CancellationToken
        {
            get
            {
                return _cancellationTokenSource.Token;
            }
        }

        public CancellationTokenSource CancellationTokenSource
        {
            get
            {
                return _cancellationTokenSource;
            }
        }

        public TraceSource GetTrace(string name) 
        {
            if(_traceManager == null)
            {
                string filename = String.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.log", _hostType, DateTime.UtcNow);
                var currentAssemblyLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
                var logPath = new DirectoryInfo(currentAssemblyLocation).Parent.FullName.ToString();

                Stream logFile = File.Create(Path.Combine(logPath, filename));
                TextWriterTraceListener traceListener = new TextWriterTraceListener(logFile);
                
                _traceManager = new TraceManager(traceListener);                    
            }
            
            return _traceManager[name];
        }

        public async Task Delay(TimeSpan delay)
        {
            await Task.Delay(delay, this.CancellationToken);
        }

        public T GetService<T>() where T : class, IAgentService
        {
            Type target;
            if (!this.serviceMappings.TryGetValue(typeof(T), out target))
            {
                CustomAttributeData attribute = typeof(T)
                    .GetTypeInfo()
                    .CustomAttributes
                    .FirstOrDefault(x => x.AttributeType == typeof(ServiceLocatorAttribute));
                if (attribute != null)
                {
                    foreach (CustomAttributeNamedArgument arg in attribute.NamedArguments)
                    {
                        if (String.Equals(arg.MemberName, ServiceLocatorAttribute.DefaultPropertyName, StringComparison.Ordinal))
                        {
                            target = arg.TypedValue.Value as Type;
                        }
                    }
                }

                if (target == null)
                {
                    throw new KeyNotFoundException(String.Format(CultureInfo.InvariantCulture, "Service mapping not found for key '{0}'.", typeof(T).FullName));
                }

                this.serviceMappings.TryAdd(typeof(T), target);
                return this.GetService<T>();
            }

            var svc = Activator.CreateInstance(target) as T;
            svc.Initialize(this);
            return svc;
        }

        public void Dispose()
        {
            if (_traceManager != null && _traceManager is IDisposable)
            {
                ((IDisposable)_traceManager).Dispose();
                _traceManager = null;    
            }
        }
    }
}