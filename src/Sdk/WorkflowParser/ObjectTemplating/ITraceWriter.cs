using System;

namespace GitHub.Actions.WorkflowParser.ObjectTemplating
{
    public interface ITraceWriter
    {
        void Error(
            String format,
            params Object[] args);

        void Info(
            String format,
            params Object[] args);

        void Verbose(
            String format,
            params Object[] args);
    }
}
