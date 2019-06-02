using System;
using System.IO;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Common.ClientStorage;
using Microsoft.VisualStudio.Services.WebApi.Internal;
using Microsoft.Win32;

namespace Microsoft.VisualStudio.Services.WebApi
{
    /// <summary>
    /// Helper for retrieving client settings which are environment-specific or retrieved from the Windows Registry
    /// </summary>
    internal static class VssClientSettings
    {
        /// <summary>
        /// Directory containing the client cache files which resides below the settings directory.
        /// 
        /// This will look something like this:
        /// C:\Documents and Settings\username\Local Settings\Application Data\Microsoft\VisualStudio Services\[GeneratedVersionInfo.TfsProductVersion]\Cache
        /// </summary>
        internal static string ClientCacheDirectory
        {
            get
            {
                return Path.Combine(ClientSettingsDirectory, "Cache");
            }
        }

        /// <summary>
        /// Directory containing the client settings files.
        /// 
        /// This will look something like this:
        /// C:\Documents and Settings\username\Local Settings\Application Data\Microsoft\VisualStudio Services\[GeneratedVersionInfo.TfsProductVersion]
        /// </summary>
        internal static string ClientSettingsDirectory
        {
            get
            {
                // We purposely do not cache this value. This value needs to change if 
                // Windows Impersonation is being used.
                return Path.Combine(VssFileStorage.ClientSettingsDirectory, GeneratedVersionInfo.TfsProductVersion);
            }
        }

#if !NETSTANDARD
        /// <summary>
        /// Defines the expiration interval for the location service client disk cache.
        /// </summary>
        internal static int? ClientCacheTimeToLive
        {
            get
            {
                if (s_clientCacheTimeToLive == null && !s_checkedClientCacheTimeToLive)
                {
                    // Check once per process lifetime, but don't keep checking the registry over and over
                    s_checkedClientCacheTimeToLive = true;

                    // Prefer HKCU over HKLM.
                    RegistryKey key = null;

                    // Default store: HKCU
                    using (RegistryKey userRoot = VssClientEnvironment.TryGetUserRegistryRoot())
                    {
                        if (userRoot != null)
                        {
                            key = userRoot.OpenSubKey(c_cacheSettingsKey);
                        }
                    }

                    // Alternate store: HKLM
                    if (key == null)
                    {
                        using (RegistryKey applicationRoot = VssClientEnvironment.TryGetApplicationRegistryRoot())
                        {
                            if (applicationRoot != null)
                            {
                                key = applicationRoot.OpenSubKey(c_cacheSettingsKey);
                            }
                        }
                    }

                    if (key != null)
                    {
                        if (key.GetValue(c_settingClientCacheTimeToLive) != null && key.GetValueKind(c_settingClientCacheTimeToLive) == RegistryValueKind.DWord)
                        {
                            s_clientCacheTimeToLive = Math.Max(1, (int)key.GetValue(c_settingClientCacheTimeToLive));
                        }
                    }
                }

                return s_clientCacheTimeToLive;
            }
            set
            {
                // For testing purposes only
                s_clientCacheTimeToLive = value;
            }
        }

        /// <summary>
        /// Gets Connect() options which are overriden in the user registry hive.
        /// </summary>
        internal static void GetConnectionOverrides(
            out VssConnectMode? connectModeOverride,
            out string userOverride)
        {
            connectModeOverride = null;
            userOverride = VssClientEnvironment.GetSharedConnectedUserValue<string>(VssConnectionParameterOverrideKeys.FederatedAuthenticationUser);

            var modeOverride = VssClientEnvironment.GetSharedConnectedUserValue<string>(VssConnectionParameterOverrideKeys.FederatedAuthenticationMode);

            VssConnectMode modeOverrideValue = VssConnectMode.Automatic;

            if (modeOverride != null && Enum.TryParse<VssConnectMode>(modeOverride, out modeOverrideValue))
            {
                connectModeOverride = modeOverrideValue;
            }
        }

        private static int? s_clientCacheTimeToLive;
        private static bool s_checkedClientCacheTimeToLive;
#endif
        private const string c_cacheSettingsKey = "Services\\CacheSettings";
        private const string c_settingClientCacheTimeToLive = "ClientCacheTimeToLive";
    }
}
