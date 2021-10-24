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
    }
}