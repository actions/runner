using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ITraceManager: IDisposable
    {
        SourceSwitch Switch { get; }
        TraceSource this[string name] { get; }
    }

    public sealed class TraceManager : ITraceManager
    {
        private readonly ConcurrentDictionary<string, TraceSource> _sources = new ConcurrentDictionary<string, TraceSource>(StringComparer.OrdinalIgnoreCase);
        private readonly TextWriterTraceListener _hostTraceListener;
        private TraceSetting _traceSetting;

        public TraceManager(TextWriterTraceListener traceListener)
            : this(traceListener, new TraceSetting())
        {
        }
        
        public TraceManager(TextWriterTraceListener traceListener, TraceSetting traceSetting)
        {
            // Validate and store params.
            ArgUtil.NotNull(traceListener, nameof(traceListener));
            ArgUtil.NotNull(traceSetting, nameof(traceSetting));
            _hostTraceListener = traceListener;
            _traceSetting = traceSetting;

            Switch = new SourceSwitch("VSTSAgentSwitch")
            {
                Level = _traceSetting.DefaultTraceLevel.ToSourceLevels()
            };
        }
        
        public SourceSwitch Switch { get; private set; }

        public TraceSource this[string name]
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
                foreach (TraceSource traceSource in _sources.Values)
                {
                    traceSource.Flush();
                    traceSource.Close();
                }

                _sources.Clear();
            }
        }

        private TraceSource CreateTraceSource(string name)
        {
            var traceSource = new TraceSource(name)
            {
                Switch = Switch
            };
            
            TraceLevel sourceTraceLevel; 
            if (_traceSetting.DetailTraceSetting.TryGetValue(name, out sourceTraceLevel))
            {
                traceSource.Switch = new SourceSwitch("VSTSAgentSubSwitch") 
                {
                    Level = sourceTraceLevel.ToSourceLevels()
                };
            }

            // Remove the default trace listener.
            if (traceSource.Listeners.Count > 0 &&
                traceSource.Listeners[0] is DefaultTraceListener)
            {
                traceSource.Listeners.RemoveAt(0);
            }

            traceSource.Listeners.Add(_hostTraceListener);
            return traceSource;
        }
    }

    public static class TraceSourceExtensions
    {
        // Do not remove the non-format overload.
        public static void Info(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Information, message);
        }

        public static void Info(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Information, StringUtil.Format(format, args));
        }

        public static void Info(this TraceSource traceSource, object item)
        {
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            Trace(traceSource, TraceEventType.Information, json);
        }

        public static void Error(this TraceSource traceSource, Exception exception)
        {
            Trace(traceSource, TraceEventType.Error, exception.ToString());
        }

        // Do not remove the non-format overload.
        public static void Error(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Error, message);
        }

        public static void Error(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Error, StringUtil.Format(format, args));
        }

        // Do not remove the non-format overload.
        public static void Warning(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Warning, message);
        }

        public static void Warning(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Warning, StringUtil.Format(format, args));
        }

        // Do not remove the non-format overload.
        public static void Verbose(this TraceSource traceSource, string message)
        {
            Trace(traceSource, TraceEventType.Verbose, message);
        }

        public static void Verbose(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Verbose, StringUtil.Format(format, args));
        }

        public static void Verbose(this TraceSource traceSource, object item)
        {
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            Trace(traceSource, TraceEventType.Verbose, json);
        }

        public static void Entering(this TraceSource traceSource, [CallerMemberName] string name = "")
        {
            Trace(traceSource, TraceEventType.Verbose, name);
        }

        private static void Trace(TraceSource traceSource, TraceEventType eventType, string message)
        {
            ArgUtil.NotNull(traceSource, nameof(traceSource));
            traceSource.TraceEvent(eventType, 0, message);
        }
    }
}