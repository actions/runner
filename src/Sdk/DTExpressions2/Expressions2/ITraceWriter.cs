using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Expressions2
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITraceWriter
    {
        void Info(String message);
        void Verbose(String message);
    }
}
