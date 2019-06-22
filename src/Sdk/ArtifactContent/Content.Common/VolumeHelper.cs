using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace GitHub.Services.Content.Common
{
    public static class VolumeHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetVolumePathName(string fileName, [Out] StringBuilder volumePathName, int bufferLength);

        public static string GetVolumeRootFromPath(string path)
        {
            var volumePath = new StringBuilder(1000);
            if (!GetVolumePathName(path, volumePath, volumePath.Capacity))
            {
                throw new Win32Exception("Failed to retreive the volume path for " + path);
            }

            return volumePath.ToString();
        }
    }
}
