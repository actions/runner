
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Worker.Handlers
{
    internal class ScriptHandlerHelpers
    {
        private static readonly Dictionary<string, string> DefaultArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cmd"] = "/D /E:ON /V:OFF /S /C \"CALL \"{0}\"\"",
            ["pwsh"] = "-command \". '{0}'\"",
            ["powershell"] = "-command \". '{0}'\"",
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

        internal static string FixUpScriptContents(string scriptType, string contents)
        {
            switch (scriptType)
            {
                case "cmd":
                    // Note, use @echo off instead of using the /Q command line switch.
                    // When /Q is used, echo can't be turned on.
                    contents = $"@echo off{Environment.NewLine}{contents}";
                    break;
                case "powershell":
                case "pwsh":
                    var prepend = "$ErrorActionPreference = 'stop'";
                    var append = @"if ((Test-Path -LiteralPath variable:\LASTEXITCODE)) { exit $LASTEXITCODE }";
                    contents = $"{prepend}{Environment.NewLine}{contents}{Environment.NewLine}{append}";
                    break;
            }
            return contents;
        }

        internal static string FormatArgumentString(string argString, string scriptFilePath)
        {
            // No args givin in options and no defaults exist. Only arg is the script file
            if (string.IsNullOrEmpty(argString))
            {
                return scriptFilePath;
            }
            else
            {
                // Format string, e.g. `-args {0}` may be given in options or come from system defaults
                if (argString.Contains("{0}"))
                {
                    return string.Format(argString, scriptFilePath);
                }
                // Regular arg string, e.g. `-abc file.sh`
                return $"{argString} {scriptFilePath}";
            }
        }

        internal static (string shellCommand, string shellArgs) ParseShellOptionString(string shellOption)
        {
            var shellStringParts = shellOption.Split(" ", 2);
            if (shellStringParts.Length == 2)
            {
                return (shellCommand: shellStringParts[0], shellArgs: shellStringParts[1]);
            }
            else if (shellStringParts.Length == 1)
            {
                return (shellCommand: shellStringParts[0], shellArgs: "");
            }
            else
            {
                throw new Exception($"Failed to parse COMMAND [..ARGS] from {shellOption}");
            }
        }
    }
}
