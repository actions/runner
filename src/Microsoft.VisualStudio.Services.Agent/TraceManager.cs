using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ITraceManager: IDisposable
    {
        SourceSwitch Switch { get; }
        TraceSourceWrapper this[string name] { get; }
    }

    public sealed class TraceManager : ITraceManager
    {
        private readonly ConcurrentDictionary<string, TraceSourceWrapper> _sources = new ConcurrentDictionary<string, TraceSourceWrapper>(StringComparer.OrdinalIgnoreCase);
        private readonly TextWriterTraceListener _hostTraceListener;
        private TraceSetting _traceSetting;
        private ISecretMasker _secretMasker;

        public TraceManager(TextWriterTraceListener traceListener, ISecretMasker secretMasker)
            : this(traceListener, new TraceSetting(), secretMasker)
        {
        }
        
        public TraceManager(TextWriterTraceListener traceListener, TraceSetting traceSetting, ISecretMasker secretMasker)
        {
            // Validate and store params.
            ArgUtil.NotNull(traceListener, nameof(traceListener));
            ArgUtil.NotNull(traceSetting, nameof(traceSetting));
            ArgUtil.NotNull(secretMasker, nameof(secretMasker));
            _hostTraceListener = traceListener;
            _traceSetting = traceSetting;
            _secretMasker = secretMasker;

            Switch = new SourceSwitch("VSTSAgentSwitch")
            {
                Level = _traceSetting.DefaultTraceLevel.ToSourceLevels()
            };
        }
        
        public SourceSwitch Switch { get; private set; }

        public TraceSourceWrapper this[string name]
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
                foreach (TraceSourceWrapper traceSource in _sources.Values)
                {
                    traceSource.Dispose();
                }

                _sources.Clear();
            }
        }

        private TraceSourceWrapper CreateTraceSource(string name)
        {
            SourceSwitch sourceSwitch = Switch;
            TraceLevel sourceTraceLevel;
            if (_traceSetting.DetailTraceSetting.TryGetValue(name, out sourceTraceLevel))
            {
                sourceSwitch = new SourceSwitch("VSTSAgentSubSwitch") 
                {
                    Level = sourceTraceLevel.ToSourceLevels()
                };
            }
            return new TraceSourceWrapper(name, _secretMasker, sourceSwitch, _hostTraceListener);
        }
    }
}
