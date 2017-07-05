using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Capabilities
{
    public sealed class AgentCapabilitiesProvider : AgentService, ICapabilitiesProvider
    {
        public Type ExtensionType => typeof(ICapabilitiesProvider);

        public int Order => 99; // Process last to override prior.

        public Task<List<Capability>> GetCapabilitiesAsync(AgentSettings settings, CancellationToken cancellationToken)
        {
            ArgUtil.NotNull(settings, nameof(settings));
            var capabilities = new List<Capability>();
            Add(capabilities, "Agent.Name", settings.AgentName ?? string.Empty);
            switch (Constants.Agent.Platform)
            {
                case Constants.OSPlatform.Linux:
                    Add(capabilities, "Agent.OS", "linux");
                    break;
                case Constants.OSPlatform.OSX:
                    Add(capabilities, "Agent.OS", "darwin");
                    break;
                case Constants.OSPlatform.Windows:
                    Add(capabilities, "Agent.OS", Environment.GetEnvironmentVariable("OS"));
                    break;
            }

#if OS_WINDOWS
            Add(capabilities, "Agent.OSVersion", GetOSVersionString());
            Add(capabilities, "Cmd", Environment.GetEnvironmentVariable("comspec"));
#endif
            var isRunningInInteractiveMode = HostContext.StartupType != StartupType.Service;
            Add(capabilities, "InteractiveSession", isRunningInInteractiveMode.ToString());
            Add(capabilities, "Agent.Version", Constants.Agent.Version);
            Add(capabilities, "Agent.ComputerName", Environment.MachineName ?? string.Empty);
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
