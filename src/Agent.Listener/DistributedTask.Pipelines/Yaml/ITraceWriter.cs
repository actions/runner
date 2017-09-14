using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ITraceWriter
    {
        void Info(String format, params Object[] args);

        void Verbose(String format, params Object[] args);
    }
}
