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
            settings.UserAgent = UserAgentUtility.GetDefaultRestUserAgent();

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
