using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
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
