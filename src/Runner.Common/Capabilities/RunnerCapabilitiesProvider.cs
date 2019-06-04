﻿using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Runner.Common.Capabilities
{
    public sealed class RunnerCapabilitiesProvider : RunnerService, ICapabilitiesProvider
    {
        public Type ExtensionType => typeof(ICapabilitiesProvider);

        public int Order => 99; // Process last to override prior.

        public Task<List<Capability>> GetCapabilitiesAsync(RunnerSettings settings, CancellationToken cancellationToken)
        {
            ArgUtil.NotNull(settings, nameof(settings));
            var capabilities = new List<Capability>();
            Add(capabilities, "Runner.Name", settings.AgentName ?? string.Empty);
            Add(capabilities, "Runner.OS", VarUtil.OS);
            Add(capabilities, "Runner.OSArchitecture", VarUtil.OSArchitecture);
#if OS_WINDOWS
            Add(capabilities, "Runner.OSVersion", GetOSVersionString());
#endif
            Add(capabilities, "InteractiveSession", (HostContext.StartupType != StartupType.Service).ToString());
            Add(capabilities, "Runner.Version", BuildConstants.RunnerPackage.Version);
            Add(capabilities, "Runner.ComputerName", Environment.MachineName ?? string.Empty);
            Add(capabilities, "Runner.HomeDirectory", HostContext.GetDirectory(WellKnownDirectory.Root));
            return Task.FromResult(capabilities);
        }

        private void Add(List<Capability> capabilities, string name, string value)
        {
            Trace.Info($"Adding '{name}': '{value}'");
            capabilities.Add(new Capability(name, value));
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

        private string GetOSVersionString()
        {
            // Do not use System.Environment.OSVersion.Version to resolve the OS version number.
            // It leverages the GetVersionEx function which may report an incorrect version
            // depending on the app's manifest. For details, see:
            //     https://msdn.microsoft.com/library/windows/desktop/ms724451(v=vs.85).aspx

            // Attempt to retrieve the major/minor version from the new registry values added in
            // in Windows 10.
            //
            // The registry value "CurrentVersion" is unreliable in Windows 10. It contains the
            // value "6.3" instead of "10.0".
            object major = GetHklmValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentMajorVersionNumber");
            object minor = GetHklmValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentMinorVersionNumber");
            string majorMinorString;
            if (major != null && minor != null)
            {
                majorMinorString = StringUtil.Format("{0}.{1}", major, minor);
            }
            else
            {
                // Fallback to the registry value "CurrentVersion".
                majorMinorString = GetHklmValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentVersion") as string;
            }

            // Opted to use the registry value "CurrentBuildNumber" over "CurrentBuild". Based on brief
            // internet investigation, the only difference appears to be that on Windows XP "CurrentBuild"
            // was unreliable and "CurrentBuildNumber" was the correct choice.
            string build = GetHklmValue(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber") as string;
            return StringUtil.Format("{0}.{1}", majorMinorString, build);
        }
    }
}
