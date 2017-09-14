using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class FileData
    {
        public String Name { get; set; }

        public String Directory { get; set; }

        public String Content { get; set; }
    }
}
