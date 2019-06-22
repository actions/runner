using System;

namespace GitHub.Services.BlobStore.Common
{
    /// <summary>
    /// Encapsulates a set of static helpers to be utilized across classes within BlobStore.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Returns a string comparer compatible for the underlying filesystem/OS.
        /// https://docs.microsoft.com/en-us/dotnet/api/system.platformid?redirectedfrom=MSDN&view=netframework-4.7.2
        /// </summary>
        /// <param name="operatingSystem"></param>
        /// <returns></returns>
        public static StringComparer FileSystemStringComparer(OperatingSystem operatingSystem)
        {
            StringComparer retStringComparer;

            switch (operatingSystem.Platform)
            {
                // Case sensitive platforms.
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    retStringComparer = StringComparer.Ordinal;
                    break;
                
                // Unsupported platform(s).
                case PlatformID.Xbox:
                    throw new PlatformNotSupportedException($"Underlying platform id : {PlatformID.Xbox} not supported");
                
                // All windows versions.
                default:
                    retStringComparer = StringComparer.OrdinalIgnoreCase;
                    break;
            }

            return retStringComparer;
        }

        /// <summary>
        /// Look up the OS platform ID and determine whether it is Windows or not.
        /// </summary>
        /// <param name="operatingSystem"></param>
        /// <returns></returns>
        public static bool IsWindowsPlatform(OperatingSystem operatingSystem)
        {
            bool ret = false;
            switch (operatingSystem.Platform)
            {
                // If the platform is one of these, return FALSE.
                // Otherwise, its Windows.
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                case PlatformID.Xbox:
                    ret = false;
                    break;

                default:
                    ret = true;
                    break;
            }

            return ret;
        }
    }
}
