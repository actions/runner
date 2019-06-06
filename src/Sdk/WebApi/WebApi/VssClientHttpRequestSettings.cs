using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security;
using GitHub.Services.Common;
using GitHub.Services.WebApi.Utilities.Internal;
using Microsoft.Win32;

namespace GitHub.Services.WebApi
{
    /// <summary>
    /// Provides access to common settings which control the behavior of requests for a <c>VssHttpClient</c> instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class VssClientHttpRequestSettings : VssHttpRequestSettings
    {
        public VssClientHttpRequestSettings()
            : base()
        {
        }

        private VssClientHttpRequestSettings(VssClientHttpRequestSettings settingsToBeCloned)
            : base(settingsToBeCloned)
        {
        }

        /// <summary>
        /// Gets the default request settings.
        /// </summary>
        public static VssClientHttpRequestSettings Default => s_defaultSettings.Value;

        public VssClientHttpRequestSettings Clone()
        {
            return new VssClientHttpRequestSettings(this);
        }

        /// <summary>
        /// Reload the defaults from the Registry.
        /// </summary>
        internal static void ResetDefaultSettings()
        {
            s_defaultSettings = new Lazy<VssClientHttpRequestSettings>(ConstructDefaultSettings);
        }

        /// <summary>
        /// Creates an instance of the default request settings.
        /// </summary>
        /// <returns>The default request settings</returns>
        private static VssClientHttpRequestSettings ConstructDefaultSettings()
        {
            // Set up reasonable defaults in case the registry keys are not present
            var settings = new VssClientHttpRequestSettings();

#if !NETSTANDARD
            try
            {
                // Prefer HKCU over HKLM.
                RegistryKey key = null;

                // Default store: HKCU
                using (RegistryKey userRoot = VssClientEnvironment.TryGetUserRegistryRoot())
                {
                    if (userRoot != null)
                    {
                        key = userRoot.OpenSubKey(c_settingsKey);
                    }
                }

                // Alternate store: HKLM
                if (key == null)
                {
                    using (RegistryKey applicationRoot = VssClientEnvironment.TryGetApplicationRegistryRoot())
                    {
                        if (applicationRoot != null)
                        {
                            key = applicationRoot.OpenSubKey(c_settingsKey);
                        }
                    }
                }

                // If no store, create the default store
                if (key == null)
                {
                    using (RegistryKey userRoot = VssClientEnvironment.TryGetUserRegistryRoot())
                    {
                        if (userRoot != null)
                        {
                            key = userRoot.CreateSubKey(c_settingsKey);
                        }
                    }

                    // Write defaults
                    String defaultAgentId = String.Format(CultureInfo.InvariantCulture, "VSS: {0}", Guid.NewGuid().ToString("D"));
                    key.SetValue(c_settingsAgentId, defaultAgentId);
                }

                if (key != null)
                {
                    using (key)
                    {
                        Boolean boolValue;

                        if (Boolean.TryParse(key.GetValue(c_settingBypassProxyOnLocal) as String, out boolValue))
                        {
                            settings.BypassProxyOnLocal = boolValue;
                        }

                        if (Boolean.TryParse(key.GetValue(c_settingEnableCompression) as String, out boolValue))
                        {
                            settings.CompressionEnabled = boolValue;
                        }

                        if (key.GetValue(c_settingsDefaultTimeout) != null && key.GetValueKind(c_settingsDefaultTimeout) == RegistryValueKind.DWord)
                        {
                            settings.SendTimeout = TimeSpan.FromMilliseconds(Math.Max(1, (Int32)key.GetValue(c_settingsDefaultTimeout)));
                        }

                        if (key.GetValue(c_settingsAgentId) != null && key.GetValueKind(c_settingsAgentId) == RegistryValueKind.String)
                        {
                            settings.AgentId = (String)key.GetValue(c_settingsAgentId);
                        }
                    }
                }

                String bypass = Environment.GetEnvironmentVariable("TFS_BYPASS_PROXY_ON_LOCAL");
                if (!String.IsNullOrEmpty(bypass))
                {
                    settings.BypassProxyOnLocal = String.Equals(bypass, "1", StringComparison.Ordinal);
                }
            }
            catch (Exception e)
            {
                // If the current account doesn't have privileges to access the registry (e.g. TFS service account)
                // ignore any registry access errors...
                if (!(e is SecurityException || e is UnauthorizedAccessException))
                {
                    Trace.WriteLine("An exception was encountered and ignored while reading settings: " + e);
                }
            }
#endif

            settings.UserAgent = UserAgentUtility.GetDefaultRestUserAgent();

#if !NETSTANDARD
            //default this to true on client\server connections
            settings.ClientCertificateManager = VssClientCertificateManager.Instance;
#endif
            return settings;
        }
        
        private static Lazy<VssClientHttpRequestSettings> s_defaultSettings 
            = new Lazy<VssClientHttpRequestSettings>(ConstructDefaultSettings);

        private const String c_settingsKey = "Services\\RequestSettings";
        private const String c_settingBypassProxyOnLocal = "BypassProxyOnLocal";
        private const String c_settingEnableCompression = "EnableCompression";
        private const String c_settingsDefaultTimeout = "DefaultTimeout";
        private const String c_settingsAgentId = "AgentId";
    }
}
