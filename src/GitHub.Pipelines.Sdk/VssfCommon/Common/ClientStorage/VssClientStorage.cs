using System.ComponentModel;

namespace Microsoft.VisualStudio.Services.Common.ClientStorage
{
    [EditorBrowsable(EditorBrowsableState.Never)] // for internal use
    public class VssClientStorage
    {
        /// <summary>
        /// General client settings that need to persist across processes.
        /// </summary>
        public static IVssClientStorage CurrentUserSettings
        {
            get
            {
                return VssFileStorage.GetCurrentUserVssFileStorage("settings.json", false);
            }
        }

        /// <summary>
        /// General client settings specific to this binaries current major version that need to persist across processes.
        /// </summary>
        public static IVssClientStorage VersionedCurrentUserSettings
        {
            get
            {
                return VssFileStorage.GetCurrentUserVssFileStorage("settings.json", true);
            }
        }
    }
}