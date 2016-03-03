using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class TestHostContext : IHostContext, IDisposable
    {
        private readonly ConcurrentDictionary<Type, ConcurrentQueue<object>> _serviceInstances = new ConcurrentDictionary<Type, ConcurrentQueue<object>>();
        private readonly ConcurrentDictionary<Type, object> _serviceSingletons = new ConcurrentDictionary<Type, object>();
        private readonly ITraceManager _traceManager;
        private readonly Terminal _term;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private string _suiteName;
        private string _testName;
                
        public TestHostContext(string suiteName, [CallerMemberName] string testName = "")
        {
            if (string.IsNullOrEmpty(suiteName))
            {
                throw new ArgumentNullException(nameof(suiteName));
            }

            if (string.IsNullOrEmpty(testName))
            {
                throw new ArgumentNullException(nameof(testName));
            }

            _suiteName = suiteName;
            _testName = testName;

            // Setup the trace manager.
            string traceFileName = $"trace_{_suiteName}_{_testName}.log";
            if (File.Exists(traceFileName))
            {
                File.Delete(traceFileName);
            }

            Stream logFile = File.Create(traceFileName);
            var traceListener = new TextWriterTraceListener(logFile);
            _traceManager = new TraceManager(traceListener);
            
            // inject a terminal in silent mode so all console output
            // goes to the test trace file
            _term = new Terminal();
            _term.Silent = true;
            SetSingleton<ITerminal>(_term);
            EnqueueInstance<ITerminal>(_term);
            
            Variables = new Variables(this);
        }

        public Variables Variables { get; private set; }
        public CancellationToken CancellationToken
        {
            get
            {
                return _cancellationTokenSource.Token;
            }
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task Delay(TimeSpan delay)
        {
            await Task.Delay(TimeSpan.Zero);
        }

        public T CreateService<T>() where T: class, IAgentService
        {
            // Dequeue a registered instance.
            object service;
            ConcurrentQueue<object> queue = _serviceInstances[typeof(T)];
            if (queue == null || !queue.TryDequeue(out service))
            {
                throw new Exception($"Unable to dequeue a registered instance for type '{typeof(T).FullName}'.");
            }

            var s = service as T;
            s.Initialize(this);
            return s;
        }

        public T GetService<T>() where T : class, IAgentService
        {
            // Get the registered singleton instance.
            T service = _serviceSingletons[typeof(T)] as T;
            if (object.ReferenceEquals(service, null))
            {
                throw new Exception($"Singleton instance not registered for type '{typeof(T).FullName}'.");
            }

            service.Initialize(this);
            return service;
        }

        public void EnqueueInstance<T>(T instance) where T : class, IAgentService
        {
            // Enqueue a service instance to be returned by CreateService.
            if (object.ReferenceEquals(instance, null))
            {
                throw new ArgumentNullException(nameof(instance));
            }

            ConcurrentQueue<object> queue = _serviceInstances.GetOrAdd(
                key: typeof(T),
                valueFactory: x => new ConcurrentQueue<object>());
            queue.Enqueue(instance);
        }

        public void SetSingleton<T>(T singleton) where T : class, IAgentService
        {
            // Set the singleton instance to be returned by GetService.
            if (object.ReferenceEquals(singleton, null))
            {
                throw new ArgumentNullException(nameof(singleton));
            }

            _serviceSingletons[typeof(T)] = singleton;
        }

        // simple convenience factory so each suite/test gets a different trace file per run
        public TraceSource GetTrace()
        {
            TraceSource trace = GetTrace($"{_suiteName}_{_testName}");
            trace.Info($"Starting {_testName}");
            return trace;
        }

        public TraceSource GetTrace(string name)
        {
            return _traceManager[name];
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
            }
        }
    }
}
