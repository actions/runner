using System;

namespace GitHub.DistributedTask.Expressions
{
    public interface ITraceWriter
    {
        void Info(String message);
        void Verbose(String message);
    }
}
