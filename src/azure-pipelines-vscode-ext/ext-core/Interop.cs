using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
public static partial class Interop {
    [JSImport("globalThis.href")]
    internal static partial Task<string> GetHRef();

    [JSImport("readFile", "extension.js")]
    internal static partial Task<string> ReadFile(string name);
}