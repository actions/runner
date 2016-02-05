using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface IHostContext
    {
        CancellationToken CancellationToken { get; }
        ITraceManager Trace { get; }
        T GetService<T>() where T : class;
    }
    
    public sealed class HostContext : IHostContext, IDisposable
    {
        public HostContext(String hostType)
        {
            if(String.IsNullOrEmpty(hostType))
            {
                throw new ArgumentNullException("hostType");
            }
            
            m_hostType = hostType;
            this.RegisterService<ITaskServer, TaskServer>();
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
                    m_traceManager = new TraceManager(m_hostType, null);                    
                }
                
                return m_traceManager;
            }
        }
        
        public T GetService<T>() where T : class
        {
            System.Type target;
            if (!serviceMappings.TryGetValue(typeof(T), out target))
            {
                throw new KeyNotFoundException(String.Format(CultureInfo.InvariantCulture, "Service mapping not found for key '{0}'.", typeof(T).FullName));
            }

            return Activator.CreateInstance(target) as T;
        }

        public void RegisterService<TKey, TValue>()
        {
            serviceMappings[typeof(TKey)] = typeof(TValue);
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