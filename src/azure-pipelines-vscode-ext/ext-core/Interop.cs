using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
public static partial class Interop {
    [JSImport("readFile", "extension.js")]
    internal static partial Task<string> ReadFile(JSObject handle, string repositoryAndRef, string name);
    [JSImport("message", "extension.js")]
    internal static partial Task Message(JSObject handle, int type, string message);
    [JSImport("sleep", "extension.js")]
    internal static partial Task Sleep(int time);
    [JSImport("log", "extension.js")]
    internal static partial void Log(JSObject handle, int type, string message);
    [JSImport("requestRequiredParameter", "extension.js")]
    internal static partial Task<string> RequestRequiredParameter(JSObject handle, string name);
    [JSImport("error", "extension.js")]
    internal static partial Task Error(JSObject handle, string message);
    [JSImport("autocompletelist", "extension.js")]
    internal static partial Task AutoCompleteList(JSObject handle, string json);
    [JSImport("semTokens", "extension.js")]
    internal static partial Task SemTokens(JSObject handle, int[] data);

    [JSImport("hoverResult", "extension.js")]
    internal static partial Task HoverResult(JSObject handle, string jsonRange, string conten);
}