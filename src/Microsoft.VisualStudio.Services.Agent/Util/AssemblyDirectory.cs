using System.IO;
using System.Reflection;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class AssemblyUtil
    {
        public static string AssemblyDirectory
        {
            get
            {
                return Path.GetDirectoryName(typeof(AssemblyUtil).GetTypeInfo().Assembly.Location);
            }
        }
    }
}
