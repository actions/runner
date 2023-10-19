using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
public static partial class Interop {
    [JSImport("readFile", "extension.js")]
    internal static partial Task<string> ReadFile(JSObject handle, string repositoryAndRef, string name);
    [JSImport("message", "extension.js")]
    internal static partial Task Message(int type, string message);
    [JSImport("sleep", "extension.js")]
    internal static partial Task Sleep(int time);
}