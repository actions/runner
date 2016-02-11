using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent
{
    public interface ITraceManager: IDisposable
    {
        SourceSwitch Switch { get; }
        TraceSource this[string name] { get; }
    }

    public class TraceManager : ITraceManager
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
            
            m_hostTraceListener = traceListener;
            m_traceSetting = traceSetting;
            
            Switch = new SourceSwitch("VSTSAgentSwitch")
            {
                Level = m_traceSetting.DefaultTraceLevel.ToSourceLevels()
            };
        }
        
        public SourceSwitch Switch { get; private set; }

        public TraceSource this[string name]
        {
            get
            {
                return m_sources.GetOrAdd(name, key => CreateTraceSource(key));
            }
        }

        public void Dispose()
        {
            if(!m_sources.IsEmpty)
            {
                foreach (var traceSource in m_sources)
                {
                    traceSource.Value.Flush();
                    traceSource.Value.Close();
                }    
                
                m_sources.Clear();            
            }
        }
        
        private TraceSource CreateTraceSource(string name)
        {
            var traceSource = new TraceSource(name)
            {
                Switch = Switch
            };
            
            TraceLevel sourceTraceLevel; 
            if(m_traceSetting.DetailTraceSetting.TryGetValue(name, out sourceTraceLevel))
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
                
            traceSource.Listeners.Add(m_hostTraceListener);
            return traceSource;
        }
        
        private TraceSetting m_traceSetting;
        private readonly ConcurrentDictionary<string, TraceSource> m_sources = new ConcurrentDictionary<string, TraceSource>(StringComparer.OrdinalIgnoreCase);
        private readonly TextWriterTraceListener m_hostTraceListener;
    }
    
    public static class TraceSourceExtensions
    {        
        public static void Info(this TraceSource traceSource, string format, params object[] args)
        {
            Trace(traceSource, TraceEventType.Information, format, args);
        }
        
        public static void Error(this TraceSource traceSource, Exception exception)
        {
            Trace(traceSource, TraceEventType.Error, exception.Message);
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

        private static void Trace(TraceSource traceSource, TraceEventType eventType, string format, params object[] args)
        {
            String message = StringUtil.Format(format, args);
            traceSource.TraceEvent(eventType, 0, message);
        }
    }
}