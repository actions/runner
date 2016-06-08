using Microsoft.VisualStudio.Services.Agent.Worker;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Loader;
using System.Reflection;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public sealed class TestHostContext : IHostContext, IDisposable
    {
        private readonly ConcurrentDictionary<Type, ConcurrentQueue<object>> _serviceInstances = new ConcurrentDictionary<Type, ConcurrentQueue<object>>();
        private readonly ConcurrentDictionary<Type, object> _serviceSingletons = new ConcurrentDictionary<Type, object>();
        private readonly ITraceManager _traceManager;
        private readonly Terminal _term;
        private readonly SecretMasker _secretMasker;
        private string _suiteName;
        private string _testName;
        private Tracing _trace;
        private AssemblyLoadContext _loadContext;

        public event EventHandler Unloading;

        public TestHostContext(object testClass, [CallerMemberName] string testName = "")
        {
            ArgUtil.NotNull(testClass, nameof(testClass));
            ArgUtil.NotNullOrEmpty(testName, nameof(testName));
            _loadContext = AssemblyLoadContext.GetLoadContext(typeof(TestHostContext).GetTypeInfo().Assembly);
            _loadContext.Unloading += LoadContext_Unloading;
            _testName = testName;

            // Trim the test assembly's root namespace from the test class's full name.
            _suiteName = testClass.GetType().FullName.Substring(
                startIndex: typeof(Tests.Program).FullName.LastIndexOf(nameof(Program)));
            _suiteName = _suiteName.Replace(".", "_");

            // Setup the trace manager.
            TraceFileName = Path.Combine(
                IOUtil.GetBinPath(),
                $"trace_{_suiteName}_{_testName}.log");
            if (File.Exists(TraceFileName))
            {
                File.Delete(TraceFileName);
            }

            var traceListener = new HostTraceListener(TraceFileName);
            _secretMasker = new SecretMasker();
            _traceManager = new TraceManager(traceListener, _secretMasker);
            _trace = GetTrace(nameof(TestHostContext));
            SetSingleton<ISecretMasker>(_secretMasker);

            // inject a terminal in silent mode so all console output
            // goes to the test trace file
            _term = new Terminal();
            _term.Silent = true;
            SetSingleton<ITerminal>(_term);
            EnqueueInstance<ITerminal>(_term);
        }

        public CultureInfo DefaultCulture { get; private set; }

        public string TraceFileName { get; private set; }

        public async Task Delay(TimeSpan delay, CancellationToken token)
        {
            await Task.Delay(TimeSpan.Zero);
        }

        public T CreateService<T>() where T: class, IAgentService
        {
            _trace.Verbose($"Create service: '{typeof(T).Name}'");

            // Dequeue a registered instance.
            object service;
            ConcurrentQueue<object> queue;
            if (!_serviceInstances.TryGetValue(typeof(T), out queue) ||
                !queue.TryDequeue(out service))
            {
                throw new Exception($"Unable to dequeue a registered instance for type '{typeof(T).FullName}'.");
            }

            var s = service as T;
            s.Initialize(this);
            return s;
        }

        public T GetService<T>() where T : class, IAgentService
        {
            _trace.Verbose($"Get service: '{typeof(T).Name}'");

            // Get the registered singleton instance.
            object service;
            if (!_serviceSingletons.TryGetValue(typeof(T), out service))
            {
                throw new Exception($"Singleton instance not registered for type '{typeof(T).FullName}'.");
            }

            T s = service as T;
            s.Initialize(this);
            return s;
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

        public void SetDefaultCulture(string name)
        {
            DefaultCulture = new CultureInfo(name);
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
        public Tracing GetTrace()
        {
            Tracing trace = GetTrace($"{_suiteName}_{_testName}");
            trace.Info($"Starting {_testName}");
            return trace;
        }

        public Tracing GetTrace(string name)
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
                if (_loadContext != null)
                {
                    _loadContext.Unloading -= LoadContext_Unloading;
                    _loadContext = null;
                }
                _traceManager?.Dispose();
            }
        }

        private void LoadContext_Unloading(AssemblyLoadContext obj)
        {
            if (Unloading != null)
            {
                Unloading(this, null);
            }
        }
    }
}
