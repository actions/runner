
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Worker.Handlers
{
    internal class ScriptHandlerHelpers
    {
        internal enum WellKnownScriptRunners
        {
            cmd,
            powershell
        }
        // internal static string GetDefaultScriptArguments(WellKnownScriptRunners scriptType, string scriptFilePath)
        // {
        //     return "";
        // }

        private static readonly Dictionary<string, string> DefaultArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cmd"] = "/D /E:ON /V:OFF /S /C \"CALL \"{0}\"\"",
            ["powershell"] = "-command \"& '{0}'\"",
            ["bash"] = "--noprofile --norc {0}",
            ["sh"] = "--noprofile --norc {0}"
        };

        private static readonly Dictionary<string, string> Extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cmd"] = ".cmd",
            ["powershell"] = ".ps1",
            ["bash"] = ".sh",
            ["sh"] = ".sh"
        };

        // TODO add lookup for wellknown file ext, needed for windows exec cmd and powershell

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