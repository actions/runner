using System;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class MockHostContext : IHostContext
    {
        public MockHostContext()
        {
            // Clear the default production service mappings added by the base constructor.
            this.serviceMappings.Clear();
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return this.cancellationToken;
            }
        }
        
        public T GetService<T>() where T : class
        {
            System.Type target;
            if (!this.serviceMappings.TryGetValue(typeof(T), out target))
            {
                throw new KeyNotFoundException(String.Format("Service mapping not found for key '{0}'.", typeof(T).FullName));
            }

            if(this.serviceInstances[typeof(T)] as T == null)
            {
                return Activator.CreateInstance(target) as T;    
            }
            else
            {
                return this.serviceInstances[typeof(T)] as T;
            }
        }

        // Register a singleton for unit testing.
        public void RegisterService<TKey, TValue>(Object instance)
        {
            this.serviceMappings[typeof(TKey)] = typeof(TValue);
            this.serviceInstances[typeof(TKey)] = instance; // Register the singleton.
        }
        
        public ITraceManager Trace 
        { 
            get
            {
                if(m_traceManager == null)
                {    
                    m_traceManager = new TraceManager("Mock", null);                    
                }
                
                return m_traceManager;
            }
        }
        
        private ITraceManager m_traceManager;    
        private readonly ConcurrentDictionary<Type, Object> serviceInstances = new ConcurrentDictionary<Type, Object>();
        private readonly ConcurrentDictionary<Type, Type> serviceMappings = new ConcurrentDictionary<Type, Type>();
        private readonly CancellationToken cancellationToken = new CancellationToken();
    }
}
