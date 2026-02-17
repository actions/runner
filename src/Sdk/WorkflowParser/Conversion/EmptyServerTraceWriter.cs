using System;

namespace GitHub.Actions.WorkflowParser.Conversion
{
    internal sealed class EmptyServerTraceWriter : IServerTraceWriter
    {
        public void TraceAlways(
            Int32 tracepoint,
            String format,
            params Object[] arguments)
        {
        }
    }
}
