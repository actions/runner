using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Util
{
    public static class VarUtil
    {
        public static StringComparer EnvironmentVariableKeyComparer
        {
            get
            {
                switch (Constants.Runner.Platform)
                {
                    case Constants.OSPlatform.Linux:
                    case Constants.OSPlatform.OSX:
                        return StringComparer.Ordinal;
                    case Constants.OSPlatform.Windows:
                        return StringComparer.OrdinalIgnoreCase;
                    default:
                        throw new NotSupportedException(); // Should never reach here.
                }
            }
        }

        public static string OS
        {
            get
            {
                switch (Constants.Runner.Platform)
                {
                    case Constants.OSPlatform.Linux:
                        return "Linux";
                    case Constants.OSPlatform.OSX:
                        return "macOS";
                    case Constants.OSPlatform.Windows:
                        return "Windows";
                    default:
                        throw new NotSupportedException(); // Should never reach here.
                }
            }
        }

        public static string OSArchitecture
        {
            get
            {
                switch (Constants.Runner.PlatformArchitecture)
                {
                    case Constants.Architecture.X86:
                        return "X86";
                    case Constants.Architecture.X64:
                        return "X64";
                    case Constants.Architecture.Arm:
                        return "ARM";
                    case Constants.Architecture.Arm64:
                        return "ARM64";
                    default:
                        throw new NotSupportedException(); // Should never reach here.
                }
            }
        }
    }
}
