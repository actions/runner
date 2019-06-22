using System;
using System.Diagnostics;

namespace GitHub.Services.Content.Common.Tracing
{
    public class NoopAppTraceSource : IAppTraceSource
    {
        public static NoopAppTraceSource Instance = new NoopAppTraceSource();

        private NoopAppTraceSource() { }

        public TraceListenerCollection Listeners
        {
            get { return null; }
        }

        public bool HasError => false;

        public SourceLevels SwitchLevel => SourceLevels.Off;

        public void AddConsoleTraceListener()
        {
        }

        public void AddFileTraceListener(string fullFileName)
        {
        }

        public void Critical(string format, params object[] args)
        {
        }

        public void Critical(int id, string format, params object[] args)
        {
        }

        public void Critical(Exception ex)
        {
        }

        public void Critical(int id, Exception ex)
        {
        }

        public void Critical(Exception ex, string format, params object[] args)
        {
        }

        public void Critical(int id, Exception ex, string format, params object[] args)
        {
        }

        public void Error(string format, params object[] args)
        {
        }

        public void Error(int id, string format, params object[] args)
        {
        }

        public void Error(Exception ex)
        {
        }

        public void Error(int id, Exception ex)
        {
        }

        public void Error(Exception ex, string format, params object[] args)
        {
        }

        public void Error(int id, Exception ex, string format, params object[] args)
        {
        }

        public void Info(string format, params object[] args)
        {
        }

        public void Info(int id, string format, params object[] args)
        {
        }

        public void TraceEvent(TraceEventType eventType, int id)
        {
        }

        public void TraceEvent(TraceEventType eventType, int id, string message)
        {
        }

        public void TraceEvent(TraceEventType eventType, int id, string format, params object[] args)
        {
        }

        public void Verbose(string format, params object[] args)
        {
        }

        public void Verbose(int id, string format, params object[] args)
        {
        }

        public void Verbose(Exception ex)
        {
        }

        public void Verbose(int id, Exception ex)
        {
        }

        public void Verbose(Exception ex, string format, params object[] args)
        {
        }

        public void Verbose(int id, Exception ex, string format, params object[] args)
        {
        }

        public void Warn(string format, params object[] args)
        {
        }

        public void Warn(int id, string format, params object[] args)
        {
        }

        public void Warn(Exception ex)
        {
        }

        public void Warn(int id, Exception ex)
        {
        }

        public void Warn(Exception ex, string format, params object[] args)
        {
        }

        public void Warn(int id, Exception ex, string format, params object[] args)
        {
        }
        
        public void ResetErrorDetection()
        {
            // NO-OP
        }
    }
}
