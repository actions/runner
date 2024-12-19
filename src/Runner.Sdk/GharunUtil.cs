using System;
using System.IO;
using System.Reflection;

namespace GitHub.Runner.Sdk {
    public class GharunUtil {
        private static bool IsUsableLocalStorage(string localStorage) {
            return localStorage != "" && !localStorage.Contains(' ') && !localStorage.Contains('"') && !localStorage.Contains('\'');
        }

        private static string GetLocalStorageLocation(string name) {
            var localStorage = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if(!IsUsableLocalStorage(localStorage)) {
                localStorage = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
            if(!IsUsableLocalStorage(localStorage)) {
                localStorage = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if(IsUsableLocalStorage(localStorage)) {
                    localStorage = Path.Join(localStorage, ".local", "share");
                }
            }
            if(!IsUsableLocalStorage(localStorage)) {
                localStorage = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
            if(!IsUsableLocalStorage(localStorage)) {
                localStorage = Path.GetTempPath();
            }
            return Path.GetFullPath(Path.Join(localStorage, name));
        }

        public static string GetLocalStorage() {
            string current = GetLocalStorageLocation("runner.server");
            if(Directory.Exists(current)) {
                return current;
            }
            string legacy = GetLocalStorageLocation("gharun");
            if(Directory.Exists(legacy)) {
                return legacy;
            }
            Directory.CreateDirectory(current);
            return current;
        }

        public static string GetHostOS() {
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) {
                return "linux";
            } else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                return "windows";
            } else if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) {
                return "osx";
            }
            return "unsupported";
        }
    }
}