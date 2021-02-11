using System;
using System.Runtime.InteropServices;

namespace GitHub.Runner.Sdk
{
    public static class BuildConstants
    {
        public static class Source
        {
            public static readonly string CommitHash = ThisAssembly.GitCommitId;
        }

        public static class RunnerPackage
        {
            public static readonly string PackageName = GetRuntimeIdentifier();
            public static readonly string Version = ThisAssembly.AssemblyVersion;

            private static string GetRuntimeIdentifier()
            {
                var architecture = RuntimeInformation.OSArchitecture;
#if OS_WINDOWS
                return $"win-{architecture.ToString().ToLower()}";
#elif OS_LINUX
                return $"linux-{architecture.ToString().ToLower()}";
#elif OS_OSX
            return $"osx-{architecture.ToString().ToLower()}";
#else
                return "unkonwn";
#endif
            }
        }
    }
}
