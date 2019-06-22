using System.IO;

namespace GitHub.Services.Content.Common
{
    public abstract class FileSystemBase
    {
        public string GetRandomFileName()
        {
            return Path.GetRandomFileName();
        }
    }
}
