using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using GitHub.DistributedTask.Logging;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common
{
    public interface ITraceManager : IDisposable
    {
        SourceSwitch Switch { get; }
        Tracing this[string name] { get; }
    }

    public sealed class TraceManager : ITraceManager
    {
        private readonly ConcurrentDictionary<string, Tracing> _sources = new(StringComparer.OrdinalIgnoreCase);
        private readonly HostTraceListener _hostTraceListener;
        private readonly StdoutTraceListener _stdoutTraceListener;
        private TraceSetting _traceSetting;
        private ISecretMasker _secretMasker;

        public TraceManager(HostTraceListener traceListener, StdoutTraceListener stdoutTraceListener, ISecretMasker secretMasker)
            : this(traceListener, stdoutTraceListener, new TraceSetting(), secretMasker)
        {
        }

        public TraceManager(HostTraceListener traceListener, StdoutTraceListener stdoutTraceListener, TraceSetting traceSetting, ISecretMasker secretMasker)
        {
            // Validate and store params.
            ArgUtil.NotNull(traceListener, nameof(traceListener));
            ArgUtil.NotNull(traceSetting, nameof(traceSetting));
            ArgUtil.NotNull(secretMasker, nameof(secretMasker));
            _hostTraceListener = traceListener;
            _stdoutTraceListener = stdoutTraceListener;
            _traceSetting = traceSetting;
            _secretMasker = secretMasker;

            Switch = new SourceSwitch("GitHubActionsRunnerSwitch")
            {
                Level = _traceSetting.DefaultTraceLevel.ToSourceLevels()
            };
        }

        public SourceSwitch Switch { get; private set; }

        public Tracing this[string name]
        {
            get
            {
                return _sources.GetOrAdd(name, key => CreateTraceSource(key));
            }
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
                foreach (Tracing traceSource in _sources.Values)
                {
                    traceSource.Dispose();
                }

                _sources.Clear();
            }
        }

        private Tracing CreateTraceSource(string name)
        {
            SourceSwitch sourceSwitch = Switch;

            TraceLevel sourceTraceLevel;
            if (_traceSetting.DetailTraceSetting.TryGetValue(name, out sourceTraceLevel))
            {
                sourceSwitch = new SourceSwitch("GitHubActionsRunnerSubSwitch")
                {
                    Level = sourceTraceLevel.ToSourceLevels()
                };
            }
            return new Tracing(name, _secretMasker, sourceSwitch, _hostTraceListener, _stdoutTraceListener);
        }
    }
}
