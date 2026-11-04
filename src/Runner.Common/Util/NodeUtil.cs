using System.Collections.ObjectModel;

namespace GitHub.Runner.Common.Util
{
    public static class NodeUtil
    {
        public static readonly ReadOnlyCollection<string> BuiltInNodeVersions = new(new[] { "node24" });

        public static string GetInternalNodeVersion()
        {
            return "node24";
        }
    }
}
