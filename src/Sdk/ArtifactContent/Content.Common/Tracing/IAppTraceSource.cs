using System;
using System.Diagnostics;

namespace GitHub.Services.Content.Common.Tracing
{
    public interface IAppTraceSource
    {
        TraceListenerCollection Listeners { get; }

        SourceLevels SwitchLevel { get; }

        bool HasError { get; }

        void AddConsoleTraceListener();

        void AddFileTraceListener(String fullFileName);

        void ResetErrorDetection();

        void Info(String format, params object[] args);

        void Info(int id, String format, params object[] args);

        void Warn(String format, params object[] args);

        void Warn(int id, String format, params object[] args);

        void Warn(Exception ex);

        void Warn(int id, Exception ex);

        void Warn(Exception ex, String format, params object[] args);

        void Warn(int id, Exception ex, String format, params object[] args);

        void Error(string format, params object[] args);

        void Error(int id, string format, params object[] args);

        void Error(Exception ex);

        void Error(int id, Exception ex);

        void Error(Exception ex, string format, params object[] args);

        void Error(int id, Exception ex, string format, params object[] args);

        void Critical(string format, params object[] args);

        void Critical(int id, string format, params object[] args);

        void Critical(Exception ex);

        void Critical(int id, Exception ex);

        void Critical(Exception ex, string format, params object[] args);

        void Critical(int id, Exception ex, string format, params object[] args);

        void Verbose(string format, params object[] args);

        void Verbose(int id, string format, params object[] args);

        void Verbose(Exception ex);

        void Verbose(int id, Exception ex);

        void Verbose(Exception ex, string format, params object[] args);

        void Verbose(int id, Exception ex, string format, params object[] args);

        /// <summary>
        ///     Writes a trace event message to the trace listeners in the System.Diagnostics.TraceSource.Listeners
        ///     collection using the specified event type and event identifier.
        /// </summary>
        /// <param name="eventType">One of the enumeration values that specifies the event type of the trace data.</param>
        /// <param name="id">A numeric identifier for the event.</param>
        /// <exception cref="ObjectDisposedException">An attempt was made to trace an event during finalization.</exception>
        void TraceEvent(TraceEventType eventType, int id);

        /// <summary>
        ///     Writes a trace event message to the trace listeners in the System.Diagnostics.TraceSource.Listeners
        ///     collection using the specified event type, event identifier, and message.
        /// </summary>
        /// <param name="eventType">One of the enumeration values that specifies the event type of the trace data.</param>
        /// <param name="id">A numeric identifier for the event.</param>
        /// <param name="message">The trace message to write.</param>
        /// <exception cref="ObjectDisposedException">An attempt was made to trace an event during finalization.</exception>
        void TraceEvent(TraceEventType eventType, int id, string message);

        /// <summary>
        ///    Writes a trace event to the trace listeners in the System.Diagnostics.TraceSource.Listeners
        ///     collection using the specified event type, event identifier, and argument array
        ///     and format.
        /// </summary>
        /// <param name="eventType">One of the enumeration values that specifies the event type of the trace data.</param>
        /// <param name="id">A numeric identifier for the event.</param>
        /// <param name="format">A composite format string (see Remarks) that contains text intermixed with zero
        ///     or more format items, which correspond to objects in the args array.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        /// <exception cref="ArgumentNullException">format is null</exception>
        /// <exception cref="FormatException">format is invalid.-or- The number that indicates an argument to format is less
        ///     than zero, or greater than or equal to the number of specified objects to format.</exception>
        /// <exception cref="ObjectDisposedException">An attempt was made to trace an event during finalization.</exception>
        void TraceEvent(TraceEventType eventType, int id, string format, params object[] args);
    }
}
