using System;

namespace GitHub.Actions.WorkflowParser
{
    public interface IServerTraceWriter
    {
        void TraceAlways(
            Int32 tracepoint,
            String format,
            params Object[] arguments);
    }
}
