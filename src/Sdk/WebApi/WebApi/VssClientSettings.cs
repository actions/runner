using System.IO;
using GitHub.Services.Common;
using GitHub.Services.Common.ClientStorage;

namespace GitHub.Services.WebApi
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
        /// C:\Documents and Settings\username\Local Settings\Application Data\GitHub\ActionsService\[GeneratedVersionInfo.ActionsProductVersion]\Cache
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
        /// C:\Documents and Settings\username\Local Settings\Application Data\GitHub\ActionsService\[GeneratedVersionInfo.ActionsProductVersion]
        /// </summary>
        internal static string ClientSettingsDirectory
        {
            get
            {
                // We purposely do not cache this value. This value needs to change if 
                // Windows Impersonation is being used.
                return Path.Combine(VssFileStorage.ClientSettingsDirectory, GeneratedVersionInfo.ActionsProductVersion);
            }
        }
    }
}
