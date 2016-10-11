using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Microsoft.VisualStudio.Services.Agent.Util
{
    [ServiceLocator(Default = typeof(NetFrameworkUtil))]
    public interface INetFrameworkUtil : IAgentService
    {
        bool Test(Version minVersion);
    }

    public sealed class NetFrameworkUtil : AgentService, INetFrameworkUtil
    {
        private List<Version> _versions;

        public bool Test(Version minVersion)
        {
            Trace.Entering();
            ArgUtil.NotNull(minVersion, nameof(minVersion));
            InitVersions();
            Trace.Info($"Testing for min NET Framework version: '{minVersion}'");
            return _versions.Any(x => x >= minVersion);
        }

        private void InitVersions()
        {
            // See http://msdn.microsoft.com/en-us/library/hh925568(v=vs.110).aspx for details on how to detect framework versions
            // Also see http://support.microsoft.com/kb/318785

            if (_versions != null)
            {
                return;
            }

            var versions = new List<Version>();

            // Check for install root.
            string installRoot = GetHklmValue(@"SOFTWARE\Microsoft\.NETFramework", "InstallRoot") as string;
            if (!string.IsNullOrEmpty(installRoot))
            {
                // Get the version sub key names.
                string ndpKeyName = @"SOFTWARE\Microsoft\NET Framework Setup\NDP";
                string[] versionSubKeyNames = GetHklmSubKeyNames(ndpKeyName)
                    .Where(x => x.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                foreach (string versionSubKeyName in versionSubKeyNames)
                {
                    string versionKeyName = $@"{ndpKeyName}\{versionSubKeyName}";

                    // Test for the version value.
                    string version = GetHklmValue(versionKeyName, "Version") as string;
                    if (!string.IsNullOrEmpty(version))
                    {
                        // Test for the install flag.
                        object install = GetHklmValue(versionKeyName, "Install");
                        if (!(install is int) || (int)install != 1)
                        {
                            continue;
                        }

                        // Test for the install path.
                        string installPath = Path.Combine(installRoot, versionSubKeyName);
                        Trace.Info($"Testing directory: '{installPath}'");
                        if (!Directory.Exists(installPath))
                        {
                            continue;
                        }

                        // Parse the version from the sub key name.
                        Version versionObject;
                        if (!Version.TryParse(versionSubKeyName.Substring(1), out versionObject)) // skip over the leading "v".
                        {
                            Trace.Info($"Unable to parse version from sub key name: '{versionSubKeyName}'");
                            continue;
                        }

                        Trace.Info($"Found version: {versionObject}");
                        versions.Add(versionObject);
                        continue;
                    }

                    // Test if deprecated.
                    if (string.Equals(GetHklmValue(versionKeyName, string.Empty) as string, "deprecated", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Get the profile key names.
                    string[] profileKeyNames = GetHklmSubKeyNames(versionKeyName)
                        .Select(x => $@"{versionKeyName}\{x}")
                        .ToArray();
                    foreach (string profileKeyName in profileKeyNames)
                    {
                        // Test for the version value.
                        version = GetHklmValue(profileKeyName, "Version") as string;
                        if (string.IsNullOrEmpty(version))
                        {
                            continue;
                        }

                        // Test for the install flag.
                        object install = GetHklmValue(profileKeyName, "Install");
                        if (!(install is int) || (int)install != 1)
                        {
                            continue;
                        }

                        // Test for the install path.
                        string installPath = (GetHklmValue(profileKeyName, "InstallPath") as string ?? string.Empty)
                            .TrimEnd(Path.DirectorySeparatorChar);
                        if (string.IsNullOrEmpty(installPath))
                        {
                            continue;
                        }

                        // Determine the version string.
                        //
                        // Use a range since customer might install beta/preview .NET Framework.
                        string versionString = null;
                        object releaseObject = GetHklmValue(profileKeyName, "Release");
                        if (releaseObject != null)
                        {
                            Trace.Info("Type is " + releaseObject.GetType().FullName);
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
                            else if (release > 394271)
                            {
                                versionString = "4.6.2";
                            }
                            else
                            {
                                Trace.Info($"Release '{release}' did not fall into an expected range.");
                            }
                        }

                        if (string.IsNullOrEmpty(versionString))
                        {
                            continue;
                        }

                        Trace.Info($"Interpreted version: {versionString}");
                        versions.Add(new Version(versionString));
                    }
                }
            }

            Trace.Info($"Found {versions.Count} versions:");
            foreach (Version versionObject in versions)
            {
                Trace.Info($" {versionObject}");
            }

            Interlocked.CompareExchange(ref _versions, versions, null);
        }

        private string[] GetHklmSubKeyNames(string keyName)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(keyName);
            if (key == null)
            {
                Trace.Info($"Key name '{keyName}' is null.");
                return new string[0];
            }

            try
            {
                string[] subKeyNames = key.GetSubKeyNames() ?? new string[0];
                Trace.Info($"Key name '{keyName}' contains sub keys:");
                foreach (string subKeyName in subKeyNames)
                {
                    Trace.Info($" '{subKeyName}'");
                }

                return subKeyNames;
            }
            finally
            {
                key.Dispose();
            }
        }

        private object GetHklmValue(string keyName, string valueName)
        {
            keyName = $@"HKEY_LOCAL_MACHINE\{keyName}";
            object value = Registry.GetValue(keyName, valueName, defaultValue: null);
            if (object.ReferenceEquals(value, null))
            {
                Trace.Info($"Key name '{keyName}', value name '{valueName}' is null.");
                return null;
            }

            Trace.Info($"Key name '{keyName}', value name '{valueName}': '{value}'");
            return value;
        }
    }
}
