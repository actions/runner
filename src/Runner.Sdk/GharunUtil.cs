using System;
using System.IO;
using System.Reflection;

namespace GitHub.Runner.Sdk {
    public class GharunUtil {
        public static string GetLocalStorage() {
            var localStorage = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if(localStorage == "") {
                localStorage = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            if(localStorage == "") {
                localStorage = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
            return Path.GetFullPath(Path.Join(localStorage, "gharun"));
        }

        public static string GetHostOS() {
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) {
                return "linux";
            } else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                return "windows";
            } else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) {
                return "osx";
            }
            return null;
        }
    }
}