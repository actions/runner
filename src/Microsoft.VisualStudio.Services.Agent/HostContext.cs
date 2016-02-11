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
        ITraceManager Trace { get; }
        Task Delay(TimeSpan delay);
        T GetService<T>() where T : class;
    }
    
    public sealed class HostContext : IHostContext, IDisposable
    {
        public HostContext(String hostType)
        {
            if(String.IsNullOrEmpty(hostType))
            {
                throw new ArgumentNullException(nameof(hostType));
            }
            
            m_hostType = hostType;
        }
       
        public CancellationToken CancellationToken
        {
            get
            {
                return m_cancellationToken;
            }
        }

        public ITraceManager Trace 
        { 
            get
            {
                if(m_traceManager == null)
                {
                    String filename = String.Format("{0}_{1:yyyyMMdd-HHmmss}-utc.log", m_hostType, DateTime.UtcNow);
                    Stream logFile = File.Create(filename);
                    TextWriterTraceListener traceListener = new TextWriterTraceListener(logFile);
                    
                    m_traceManager = new TraceManager(traceListener);                    
                }
                
                return m_traceManager;
            }
        }

        public async Task Delay(TimeSpan delay)
        {
            await Task.Delay(delay, this.CancellationToken);
        }

        public T GetService<T>() where T : class
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

            return Activator.CreateInstance(target) as T;
        }

        public void Dispose()
        {
            if (m_traceManager != null && m_traceManager is IDisposable)
            {
                ((IDisposable)m_traceManager).Dispose();
                m_traceManager = null;    
            }
        }
        
        private readonly ConcurrentDictionary<Type, Type> serviceMappings = new ConcurrentDictionary<Type, Type>();
        private readonly CancellationToken m_cancellationToken = new CancellationToken();
        private ITraceManager m_traceManager;
        private String m_hostType;
    }
}