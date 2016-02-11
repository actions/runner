using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class TestHostContext : IHostContext
    {
        public CancellationToken CancellationToken
        {
            get
            {
                return this.cancellationToken;
            }
        }

        public async Task Delay(TimeSpan delay)
        {
            await Task.Delay(TimeSpan.Zero);
        }

        public T GetService<T>() where T : class
        {
            return this.serviceInstances[typeof(T)] as T;
        }

        // Register a singleton for unit testing.
        public void RegisterService<TKey>(Object singleton)
        {
            this.serviceInstances[typeof(TKey)] = singleton;
        }
        
        public ITraceManager Trace 
        { 
            get
            {
                if(m_traceManager == null)
                {    
                    m_traceManager = new TraceManager();
                }
                
                return m_traceManager;
            }
        }
        
        private ITraceManager m_traceManager;    
        private readonly ConcurrentDictionary<Type, Object> serviceInstances = new ConcurrentDictionary<Type, Object>();
        private readonly CancellationToken cancellationToken = new CancellationToken();
    }
}
