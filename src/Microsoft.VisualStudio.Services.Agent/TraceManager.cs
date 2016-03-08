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
        public TraceManager()
            :this(new TextWriterTraceListener(System.Console.Out), new TraceSetting())
        {
        }
        
        public TraceManager(TextWriterTraceListener traceListener)
            :this(traceListener, new TraceSetting())
        {
        }
        
        public TraceManager(TextWriterTraceListener traceListener, TraceSetting traceSetting)
        {
            if(traceListener == null)
            {
                throw new ArgumentNullException(nameof(traceListener));
            }
            
            if(traceSetting == null)
            {
                throw new ArgumentNullException(nameof(traceSetting));
            }
            
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
            if(_traceSetting.DetailTraceSetting.TryGetValue(name, out sourceTraceLevel))
            {
                traceSource.Switch = new SourceSwitch("VSTSAgentSubSwitch") 
                {
                    Level = sourceTraceLevel.ToSourceLevels()
                };        
            }
            
            if (traceSource.Listeners.Count > 0 && 
                traceSource.Listeners[0] is DefaultTraceListener)
            {
                traceSource.Listeners.RemoveAt(0);
            }
                
            traceSource.Listeners.Add(_hostTraceListener);
            return traceSource;
        }
        
        private TraceSetting _traceSetting;
        private readonly ConcurrentDictionary<string, TraceSource> _sources = new ConcurrentDictionary<string, TraceSource>(StringComparer.OrdinalIgnoreCase);
        private readonly TextWriterTraceListener _hostTraceListener;
    }
    
    public static class TraceSourceExtensions
    {        
        public static void Info(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Information, format, args);
        }
        
        public static void Info(this TraceSource traceSource, object item, params object[] args)
        {
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            Trace(traceSource, TraceEventType.Information, json);
        }
                
        public static void Error(this TraceSource traceSource, Exception exception)
        {
            Trace(traceSource, TraceEventType.Error, exception.ToString());
        }
        
        public static void Error(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Error, format, args);
        }

        public static void Warning(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Warning, format, args);
        }

        public static void Verbose(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Verbose, format, args);
        }
        
        public static void Verbose(this TraceSource traceSource, object item, params object[] args)
        {
            string json = JsonConvert.SerializeObject(item, Formatting.Indented);
            Trace(traceSource, TraceEventType.Verbose, json);
        }        

        public static void Entering(this TraceSource traceSource, [CallerMemberName] string name = "")
        {
            traceSource.Verbose(name);
        }
        
        private static void Trace(TraceSource traceSource, TraceEventType eventType, string format, params object[] args)
        {
            ArgUtil.NotNull(traceSource, nameof(traceSource));
            String message = StringUtil.Format(format, args);
            traceSource.TraceEvent(eventType, 0, message);
        }
    }
}