using System;

namespace GitHub.Actions.Expressions
{
    public interface ITraceWriter
    {
        void Info(String message);
        void Verbose(String message);
    }
}