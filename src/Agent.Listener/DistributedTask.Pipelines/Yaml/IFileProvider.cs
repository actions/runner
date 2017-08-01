using System;
using System.ComponentModel;

namespace Microsoft.TeamFoundation.DistributedTask.Orchestration.Server.Pipelines.Yaml
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFileProvider
    {
        FileData GetFile(String path);

        String ResolvePath(String defaultRoot, String path);
    }
}
