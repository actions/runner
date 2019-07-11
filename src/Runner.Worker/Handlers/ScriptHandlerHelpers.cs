
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Worker.Handlers
{
    internal class ScriptHandlerHelpers
    {
        private static readonly Dictionary<string, string> DefaultArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cmd"] = "/D /E:ON /V:OFF /S /C \"CALL \"{0}\"\"",
            ["pwsh"] = "-command \"& '{0}'\"",
            ["powershell"] = "-command \"& '{0}'\"",
            ["bash"] = "--noprofile --norc -e -o pipefail {0}",
            ["sh"] = "-e {0}"
        };

        private static readonly Dictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cmd"] = ".cmd",
            ["pwsh"] = ".ps1",
            ["powershell"] = ".ps1",
            ["bash"] = ".sh",
            ["sh"] = ".sh"
        };

        internal static bool TryGetDefaultScriptArguments(string scriptType, string scriptFilePath, out string defaultRunArgs)
        {
            if (DefaultArguments.TryGetValue(scriptType, out var argTemplate))
            {
                defaultRunArgs = string.Format(argTemplate, scriptFilePath);
                return true;
            }
            defaultRunArgs = null;
            return false;
        }

        internal static string GetScriptArgumentsFormat(string scriptType)
        {
            if (DefaultArguments.TryGetValue(scriptType, out var argFormat))
            {
                return argFormat;
            }
            return "";
        }
        internal static string GetScriptFileExtension(string scriptType)
        {
            if (Extensions.TryGetValue(scriptType, out var extension))
            {
                return extension;
            }
            return "";
        }
    }
}
