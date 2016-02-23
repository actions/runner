using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Listener;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{

    public sealed class TestHostContext : IHostContext, IDisposable
    {

        public TestHostContext(string suiteName, [CallerMemberName] string testName = "")
        {
            if (suiteName == null)
            {
                throw new ArgumentNullException("suiteName");
            }

            if (testName == null)
            {
                throw new ArgumentNullException("testName");
            }

            m_suiteName = suiteName;
            m_testName = testName;
        }

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

        public T GetService<T>() where T : class, IAgentService
        {
            T svc = this.serviceInstances[typeof(T)] as T;
            svc.Initialize(this);
            return svc;
        }

        // Register a singleton for unit testing.
        public void RegisterService<TKey>(Object singleton)
        {
            this.serviceInstances[typeof(TKey)] = singleton;
        }
        
        // simple convenience factory so each suite/test gets a different trace file per run
        public TraceSource GetTrace()
        {
            return GetTrace(m_suiteName + '_' + m_testName);
        }

        public TraceSource GetTrace(string name)
        {
            TraceSource trace = Trace[m_suiteName + '_' + m_testName];
            trace.Info("Starting {0}", m_testName);
            return trace;
        }

        public ITraceManager Trace 
        { 
            get
            {                
                if(m_traceManager == null)
                {
                    String filename = String.Format("trace_{0}_{1}.log",
                                                    m_suiteName, 
                                                    m_testName);

                    if (File.Exists(filename)) {
                        File.Delete(filename);
                    }

                    Stream logFile = File.Create(filename);
                    TextWriterTraceListener traceListener = new TextWriterTraceListener(logFile);
                    
                    m_traceManager = new TraceManager(traceListener);
                }
                
                return m_traceManager;
            }
        }

        public void Dispose()
        {
            if (m_traceManager != null && m_traceManager is IDisposable)
            {
                ((IDisposable)m_traceManager).Dispose();
                m_traceManager = null;    
            }
        }

        private string m_suiteName;
        private string m_testName;
        private ITraceManager m_traceManager;    
        private readonly ConcurrentDictionary<Type, Object> serviceInstances = new ConcurrentDictionary<Type, Object>();
        private readonly CancellationToken cancellationToken = new CancellationToken();
    }
}
