using System;
using System.Text;
using System.Runtime.InteropServices;

namespace GitHub.Services.Content.Common.Telemetry
{
    /// <summary>
    /// Provides the means to get platform specific system information.
    /// </summary>
    public static class TelemetryPlatformSpecific
    {
        public const int DotNetReleaseDword = -1;
        internal static string FrameworkDescription => GetFrameworkDescription();
        // "Microsoft Windows 10.0.17763" => "10.0.17763"
        internal static string OSVersion => OSDescriptionConstructs[OSDescriptionConstructs.Length - 1];
        // "Microsoft Windows 10.0.17763" => "Microsoft Windows"
        internal static string OSName => ConstructDescription(OSDescriptionConstructs);
        // e.g. ["Microsoft", "Windows", "10.0.17763"]
        private static string[] OSDescriptionConstructs 
            => RuntimeInformation.OSDescription.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// From https://techblog.dorogin.com/how-to-detect-net-core-version-in-runtime-ecd65ad695be
        /// Gets and formats the current .NET Core product name.
        /// ".NET Core 4.6.26515.07" => ".NET Core 2.1.0"
        /// </summary>
        /// <returns>Formatted .NET Core product name</returns>
        private static string GetFrameworkDescription()
        {
            StringBuilder sb = new StringBuilder();
            // e.g. [".NET", "Core", "4.6.26515.07"]
            string[] frameworkDescriptionConstructs = RuntimeInformation
                .FrameworkDescription
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            // Append(".NET Core")
            sb.Append(ConstructDescription(frameworkDescriptionConstructs));
            // Append(" 2.1.0")
            sb.Append($" {GetNetCoreVersion()}");

            return sb.ToString();
        }

        /// <summary>
        /// From https://techblog.dorogin.com/how-to-detect-net-core-version-in-runtime-ecd65ad695be
        /// Iterates through descriptionConstructs until an entry is found to start with a digit
        /// Concats and returns a string of all preceding elements.
        /// </summary>
        /// <param name="descriptionConstructs">e.g. ["Microsoft", "Windows", "10.0.17763"]</param>
        /// <returns>A string comprised of all elements preceding the version number.</returns>
        private static string ConstructDescription(string[] descriptionConstructs)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < descriptionConstructs.Length; i++)
            {
                if (char.IsDigit(descriptionConstructs[i][0]))
                {
                    // Append("Microsoft Windows")
                    sb.Append(string.Join(" ", descriptionConstructs, 0, i));
                    break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// From https://techblog.dorogin.com/how-to-detect-net-core-version-in-runtime-ecd65ad695be
        /// </summary>
        /// <returns>Current .NET Core version number</returns>
        private static string GetNetCoreVersion()
        {
            var assembly = typeof(System.Runtime.GCSettings).GetType().Assembly;
            var assemblyPath = assembly.CodeBase.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            int netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
            if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2)
                return assemblyPath[netCoreAppIndex + 1];
            return string.Empty;
        }
    }
}
