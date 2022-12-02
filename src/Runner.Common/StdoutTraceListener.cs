using GitHub.Runner.Sdk;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace GitHub.Runner.Common
{
    public sealed class StdoutTraceListener : ConsoleTraceListener
    {
        // Copied and modified slightly from .Net Core source code. Modification was required to make it compile.
        // There must be some TraceFilter extension class that is missing in this source code.
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, message, null, null, null))
            {
                return;
            }

            WriteHeader(source, eventType, id);
            WriteLine(message);
            WriteFooter(eventCache);
        }

        internal bool IsEnabled(TraceOptions opts)
        {
            return (opts & TraceOutputOptions) != 0;
        }

        // Altered from the original .Net Core implementation.
        private void WriteHeader(string source, TraceEventType eventType, int id)
        {
            string type = null;
            switch (eventType)
            {
                case TraceEventType.Critical:
                    type = "CRIT";
                    break;
                case TraceEventType.Error:
                    type = "ERR ";
                    break;
                case TraceEventType.Warning:
                    type = "WARN";
                    break;
                case TraceEventType.Information:
                    type = "INFO";
                    break;
                case TraceEventType.Verbose:
                    type = "VERB";
                    break;
                default:
                    type = eventType.ToString();
                    break;
            }

            Write(StringUtil.Format("[{0:u} {1} {2}] ", DateTime.UtcNow, type, source));
        }

        // Copied and modified slightly from .Net Core source code to make it compile. The original code
        // accesses a private indentLevel field. In this code it has been modified to use the getter/setter.
        private void WriteFooter(TraceEventCache eventCache)
        {
            if (eventCache == null)
                return;

            IndentLevel++;
            if (IsEnabled(TraceOptions.ProcessId))
                WriteLine("ProcessId=" + eventCache.ProcessId);

            if (IsEnabled(TraceOptions.ThreadId))
                WriteLine("ThreadId=" + eventCache.ThreadId);

            if (IsEnabled(TraceOptions.DateTime))
                WriteLine("DateTime=" + eventCache.DateTime.ToString("o", CultureInfo.InvariantCulture));

            if (IsEnabled(TraceOptions.Timestamp))
                WriteLine("Timestamp=" + eventCache.Timestamp);

            IndentLevel--;
        }
    }
}
