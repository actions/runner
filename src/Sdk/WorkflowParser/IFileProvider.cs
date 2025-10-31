using System;

namespace GitHub.Actions.WorkflowParser
{
    public interface IFileProvider
    {
        String GetFileContent(String path);

        String ResolvePath(String defaultRoot, String path);
    }
}
