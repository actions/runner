using System;

namespace Microsoft.TeamFoundation.DistributedTask.Expressions
{
    public interface ITraceWriter
    {
        void Info(String message);
        void Verbose(String message);
    }
}