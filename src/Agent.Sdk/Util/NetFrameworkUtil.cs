using Agent.Sdk;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    public static class NetFrameworkUtil
    {
        private static List<Version> _versions;

        public static bool Test(Version minVersion, ITraceWriter trace)
        {
            ArgUtil.NotNull(minVersion, nameof(minVersion));
            InitVersions(trace);
            trace?.Info($"Testing for min NET Framework version: '{minVersion}'");
            return _versions.Any(x => x >= minVersion);
        }

        private static void InitVersions(ITraceWriter trace)
        {
            // See http://msdn.microsoft.com/en-us/library/hh925568(v=vs.110).aspx for details on how to detect framework versions
            // Also see http://support.microsoft.com/kb/318785

            if (_versions != null)
            {
                return;
            }

            var versions = new List<Version>();

            // Check for install root.
            string installRoot = GetHklmValue(@"SOFTWARE\Microsoft\.NETFramework", "InstallRoot", trace) as string;
            if (!string.IsNullOrEmpty(installRoot))
            {
                // Get the version sub key names.
                string ndpKeyName = @"SOFTWARE\Microsoft\NET Framework Setup\NDP";
                string[] versionSubKeyNames = GetHklmSubKeyNames(ndpKeyName, trace)
                    .Where(x => x.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                foreach (string versionSubKeyName in versionSubKeyNames)
                {
                    string versionKeyName = $@"{ndpKeyName}\{versionSubKeyName}";

                    // Test for the version value.
                    string version = GetHklmValue(versionKeyName, "Version", trace) as string;
                    if (!string.IsNullOrEmpty(version))
                    {
                        // Test for the install flag.
                        object install = GetHklmValue(versionKeyName, "Install", trace);
                        if (!(install is int) || (int)install != 1)
                        {
                            continue;
                        }

                        // Test for the install path.
                        string installPath = Path.Combine(installRoot, versionSubKeyName);
                        trace?.Info($"Testing directory: '{installPath}'");
                        if (!Directory.Exists(installPath))
                        {
                            continue;
                        }

                        // Parse the version from the sub key name.
                        Version versionObject;
                        if (!Version.TryParse(versionSubKeyName.Substring(1), out versionObject)) // skip over the leading "v".
                        {
                            trace?.Info($"Unable to parse version from sub key name: '{versionSubKeyName}'");
                            continue;
                        }

                        trace?.Info($"Found version: {versionObject}");
                        versions.Add(versionObject);
                        continue;
                    }

                    // Test if deprecated.
                    if (string.Equals(GetHklmValue(versionKeyName, string.Empty, trace) as string, "deprecated", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Get the profile key names.
                    string[] profileKeyNames = GetHklmSubKeyNames(versionKeyName, trace)
                        .Select(x => $@"{versionKeyName}\{x}")
                        .ToArray();
                    foreach (string profileKeyName in profileKeyNames)
                    {
                        // Test for the version value.
                        version = GetHklmValue(profileKeyName, "Version", trace) as string;
                        if (string.IsNullOrEmpty(version))
                        {
                            continue;
                        }

                        // Test for the install flag.
                        object install = GetHklmValue(profileKeyName, "Install", trace);
                        if (!(install is int) || (int)install != 1)
                        {
                            continue;
                        }

                        // Test for the install path.
                        string installPath = (GetHklmValue(profileKeyName, "InstallPath", trace) as string ?? string.Empty)
                            .TrimEnd(Path.DirectorySeparatorChar);
                        if (string.IsNullOrEmpty(installPath))
                        {
                            continue;
                        }

                        // Determine the version string.
                        //
                        // Use a range since customer might install beta/preview .NET Framework.
                        string versionString = null;
                        object releaseObject = GetHklmValue(profileKeyName, "Release", trace);
                        if (releaseObject != null)
                        {
                            trace?.Info("Type is " + releaseObject.GetType().FullName);
                        }

                        if (releaseObject is int)
                        {
                            int release = (int)releaseObject;
                            if (release == 378389)
                            {
                                versionString = "4.5.0";
                            }
                            else if (release > 378389 && release <= 378758)
                            {
                                versionString = "4.5.1";
                            }
                            else if (release > 378758 && release <= 379893)
                            {
                                versionString = "4.5.2";
                            }
                            else if (release > 379893 && release <= 380995)
                            {
                                versionString = "4.5.3";
                            }
                            else if (release > 380995 && release <= 393297)
                            {
                                versionString = "4.6.0";
                            }
                            else if (release > 393297 && release <= 394271)
                            {
                                versionString = "4.6.1";
                            }
                            else if (release > 394271 && release <= 394806)
                            {
                                versionString = "4.6.2";
                            }
                            else if (release > 394806)
                            {
                                versionString = "4.7.0";
                            }
                            else
                            {
                                trace?.Info($"Release '{release}' did not fall into an expected range.");
                            }
                        }

                        if (string.IsNullOrEmpty(versionString))
                        {
                            continue;
                        }

                        trace?.Info($"Interpreted version: {versionString}");
                        versions.Add(new Version(versionString));
                    }
                }
            }

            trace?.Info($"Found {versions.Count} versions:");
            foreach (Version versionObject in versions)
            {
                trace?.Info($" {versionObject}");
            }

            Interlocked.CompareExchange(ref _versions, versions, null);
        }

        private static string[] GetHklmSubKeyNames(string keyName, ITraceWriter trace)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(keyName);
            if (key == null)
            {
                trace?.Info($"Key name '{keyName}' is null.");
                return new string[0];
            }

            try
            {
                string[] subKeyNames = key.GetSubKeyNames() ?? new string[0];
                trace?.Info($"Key name '{keyName}' contains sub keys:");
                foreach (string subKeyName in subKeyNames)
                {
                    trace?.Info($" '{subKeyName}'");
                }

                return subKeyNames;
            }
            finally
            {
                key.Dispose();
            }
        }

        private static object GetHklmValue(string keyName, string valueName, ITraceWriter trace)
        {
            keyName = $@"HKEY_LOCAL_MACHINE\{keyName}";
            object value = Registry.GetValue(keyName, valueName, defaultValue: null);
            if (object.ReferenceEquals(value, null))
            {
                trace?.Info($"Key name '{keyName}', value name '{valueName}' is null.");
                return null;
            }

            trace?.Info($"Key name '{keyName}', value name '{valueName}': '{value}'");
            return value;
        }
    }
}
