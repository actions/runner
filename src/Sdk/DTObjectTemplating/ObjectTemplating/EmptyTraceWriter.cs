using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class EmptyTraceWriter : ITraceWriter
    {
        public void Error(
            String format,
            params Object[] args)
        {
        }

        public void Info(
            String format,
            params Object[] args)
        {
        }

        public void Verbose(
            String format,
            params Object[] args)
        {
        }
    }
}
