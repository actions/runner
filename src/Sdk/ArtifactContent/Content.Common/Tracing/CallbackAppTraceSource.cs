using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using GitHub.Services.Common;

namespace GitHub.Services.Content.Common.Tracing
{
    /// <summary>
    /// Redirects tracing to a callback if it meets or exceeds the minimum severity specified.
    /// 
    /// Methods with a signature of (string format, params object[] args) allow the args to be null or empty because
    /// the caller may have preformatted its messages or have traced messages which only look like .net format
    /// strings. We chose this behavior in favor of imposing a check for missing parameters on the caller's tracing
    /// because the purpose of this class is only to replay existing tracing, as faithfully as possible, to a callback.
    /// </summary>
    public class CallbackAppTraceSource : IAppTraceSource
    {
        private readonly Action<string, SourceLevels> traceMessageSeverityCallback;
        private readonly SourceLevels leastSevereLevelToTrace;
        private readonly bool includeSeverityLevelInMessage;

        /// <param name="traceMessageCallback">Callback which will receive traced messages</param>
        /// <param name="leastSevereLevelToTrace">
        /// Constrains tracing output to messages meeting or exceeding the given severity.
        /// SourceLevel flag values are in descending order from most severe (Critical is 1) to least (Verbose = 31 and ActivityTracing = 65280),
        /// so the filter will exclude messages with a flag value higher than the highest flag value specified here.
        /// For example, a leastSevereLevelToTrace of Warning will exclude Information and Verbose traces.
        /// </param>
        public CallbackAppTraceSource(Action<string> traceMessageCallback, SourceLevels leastSevereLevelToTrace, bool includeSeverityLevel = true)
        {
            ArgumentUtility.CheckForNull(traceMessageCallback, nameof(traceMessageCallback));

            this.traceMessageSeverityCallback = (string message, SourceLevels severity) => traceMessageCallback(message);
            this.leastSevereLevelToTrace = leastSevereLevelToTrace;
            this.includeSeverityLevelInMessage = includeSeverityLevel;
        }

        /// <param name="traceMessageSeverityCallback">Callback which will receive both the traced message and its severity</param>
        public CallbackAppTraceSource(Action<string, SourceLevels> traceMessageSeverityCallback, SourceLevels leastSevereLevelToTrace)
        {
            ArgumentUtility.CheckForNull(traceMessageSeverityCallback, nameof(traceMessageSeverityCallback));

            this.traceMessageSeverityCallback = traceMessageSeverityCallback;
            this.leastSevereLevelToTrace = leastSevereLevelToTrace;
            this.includeSeverityLevelInMessage = false;
        }

        private void TraceInternal(SourceLevels severity, string message)
        {
            if (severity == SourceLevels.All)
            {
                throw new ArgumentException(SafeStringFormat.FormatSafe($"Message's severity must not be the behavior value {nameof(SourceLevels)}.{nameof(SourceLevels.All)}"));
            }

            if (severity == SourceLevels.Off)
            {
                throw new ArgumentException(SafeStringFormat.FormatSafe($"Message's severity must not be the behavior value {nameof(SourceLevels)}.{nameof(SourceLevels.Off)}"));
            }

            // SourceLevel flag values are in descending order, so "most severe" represented as 1 and "least severe" is represented by the highest value
            var severityIsSufficientlyImportantToTrace = (int)severity <= (int)leastSevereLevelToTrace;

            if (leastSevereLevelToTrace == SourceLevels.All || severityIsSufficientlyImportantToTrace)
            {
                if (this.includeSeverityLevelInMessage)
                {
                    message = SafeStringFormat.FormatSafe($"{severity.ToString()}, {message}");
                }

                traceMessageSeverityCallback(message, severity);
            }
        }
        
        private void TraceInternal(SourceLevels severity, int? id, Exception ex, string format, params object[] args)
        {
            var message = new StringBuilder();

            if (id.HasValue)
            {
                message.Append(id);
                if (!string.IsNullOrEmpty(format) || ex != null)
                {
                    message.Append(", ");
                }
            }

            if (!string.IsNullOrEmpty(format))
            {                
                message.AppendFormatSafe(format, args);
                if (ex != null)
                {
                    message.Append(" ");
                }
            }

            if (ex != null)
            {
                message.Append(ex.ToString());
            }

            TraceInternal(severity, message.ToString());
        }

        private static Dictionary<TraceEventType, SourceLevels> toLevel;

        static CallbackAppTraceSource()
        {
            toLevel = new Dictionary<TraceEventType, SourceLevels>()
            {
                { TraceEventType.Critical, SourceLevels.Critical },
                { TraceEventType.Error, SourceLevels.Error },
                { TraceEventType.Information, SourceLevels.Information },

                { TraceEventType.Resume, SourceLevels.ActivityTracing },
                { TraceEventType.Start, SourceLevels.ActivityTracing },
                { TraceEventType.Stop, SourceLevels.ActivityTracing },
                { TraceEventType.Suspend, SourceLevels.ActivityTracing },
                { TraceEventType.Transfer, SourceLevels.ActivityTracing },

                { TraceEventType.Verbose, SourceLevels.Verbose },
                { TraceEventType.Warning, SourceLevels.Warning }
            };
        }

        private void TraceEventInternal(TraceEventType type, int? id, Exception ex, string format, params object[] args)
        {
            var message = new StringBuilder();
            var level = toLevel[type];

            if (id.HasValue)
            {
                message.Append(id);
                if (level == SourceLevels.ActivityTracing || !string.IsNullOrEmpty(format) || ex != null)
                {
                    message.Append(", ");
                }
            }
            
            if (level == SourceLevels.ActivityTracing)
            {
                message.Append(type.ToString());
                if (!string.IsNullOrEmpty(format) || ex != null)
                {
                    message.Append(", ");
                }
            }

            if (!string.IsNullOrEmpty(format))
            {
                message.AppendFormatSafe(format, args);
                if (ex != null)
                {
                    message.Append(" ");
                }
            }

            if (ex != null)
            {
                message.Append(ex.ToString());
            }

            TraceInternal(level, message.ToString());
        }

        public TraceListenerCollection Listeners
        {
            get { return null; }
        }

        public bool HasError => false;

        public SourceLevels SwitchLevel => leastSevereLevelToTrace;

        public void AddConsoleTraceListener()
        {
        }

        public void AddFileTraceListener(string fullFileName)
        {
        }

        public void Critical(string format, params object[] args)
        {
            TraceInternal(SourceLevels.Critical, null, null, format, args);
        }

        public void Critical(int id, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Critical, id, null, format, args);
        }

        public void Critical(Exception ex)
        {
            TraceInternal(SourceLevels.Critical, null, ex, null);
        }

        public void Critical(int id, Exception ex)
        {
            TraceInternal(SourceLevels.Critical, id, ex, null);
        }

        public void Critical(Exception ex, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Critical, null, ex, format, args);
        }

        public void Critical(int id, Exception ex, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Critical, id, ex, format, args);
        }

        public void Error(string format, params object[] args)
        {
            TraceInternal(SourceLevels.Error, null, null, format, args);
        }

        public void Error(int id, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Error, id, null, format, args);
        }

        public void Error(Exception ex)
        {
            TraceInternal(SourceLevels.Error, null, ex, null);
        }

        public void Error(int id, Exception ex)
        {
            TraceInternal(SourceLevels.Error, id, ex, null);
        }

        public void Error(Exception ex, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Error, null, ex, format, args);
        }

        public void Error(int id, Exception ex, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Error, id, ex, format, args);
        }

        public void Info(string format, params object[] args)
        {
            TraceInternal(SourceLevels.Information, null, null, format, args);
        }

        public void Info(int id, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Information, id, null, format, args);
        }

        public void TraceEvent(TraceEventType eventType, int id)
        {
            TraceEventInternal(eventType, id, null, null);
        }

        public void TraceEvent(TraceEventType eventType, int id, string message)
        {
            TraceEventInternal(eventType, id, null, message);
        }

        public void TraceEvent(TraceEventType eventType, int id, string format, params object[] args)
        {
            TraceEventInternal(eventType, id, null, format, args);
        }

        public void Verbose(string format, params object[] args)
        {
            TraceInternal(SourceLevels.Verbose, null, null, format, args);
        }

        public void Verbose(int id, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Verbose, id, null, format, args);
        }

        public void Verbose(Exception ex)
        {
            TraceInternal(SourceLevels.Verbose, null, ex, null);
        }

        public void Verbose(int id, Exception ex)
        {
            TraceInternal(SourceLevels.Verbose, id, ex, null);
        }

        public void Verbose(Exception ex, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Verbose, null, ex, format, args);
        }

        public void Verbose(int id, Exception ex, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Verbose, id, ex, format, args);
        }

        public void Warn(string format, params object[] args)
        {
            TraceInternal(SourceLevels.Warning, null, null, format, args);
        }

        public void Warn(int id, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Warning, id, null, format, args);
        }

        public void Warn(Exception ex)
        {
            TraceInternal(SourceLevels.Warning, null, ex, null);
        }

        public void Warn(int id, Exception ex)
        {
            TraceInternal(SourceLevels.Warning, id, ex, null);
        }

        public void Warn(Exception ex, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Warning, null, ex, format, args);
        }

        public void Warn(int id, Exception ex, string format, params object[] args)
        {
            TraceInternal(SourceLevels.Warning, id, ex, format, args);
        }

        public void ResetErrorDetection()
        {
            // NO-OP
        }
    }
}
