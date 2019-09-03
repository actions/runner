using System;
using System.ComponentModel;

namespace GitHub.DistributedTask.Pipelines.ObjectTemplating
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IFileProvider
    {
        String GetFileContent(String path);

        String ResolvePath(String defaultRoot, String path);
    }
}
