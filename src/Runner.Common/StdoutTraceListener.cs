using GitHub.Runner.Sdk;
using System;
using System.Diagnostics;
using System.Globalization;

namespace GitHub.Runner.Common
{
    public sealed class StdoutTraceListener : RunnerTraceListener
    {
        // Console.Out is the stdout stream
        public StdoutTraceListener() : base(Console.Out) { }

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
    }
}
