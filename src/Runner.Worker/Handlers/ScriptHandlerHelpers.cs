
using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Handlers
{
    internal class ScriptHandlerHelpers
    {
        private static readonly Dictionary<string, string> _defaultArguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cmd"] = "/D /E:ON /V:OFF /S /C \"CALL \"{0}\"\"",
            ["pwsh"] = "-command \". '{0}'\"",
            ["powershell"] = "-command \". '{0}'\"",
            ["bash"] = "--noprofile --norc -e -o pipefail '{0}'",
            ["sh"] = "-e '{0}'",
            ["python"] = "{0}"
        };

        private static readonly Dictionary<string, string> _extensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["cmd"] = ".cmd",
            ["pwsh"] = ".ps1",
            ["powershell"] = ".ps1",
            ["bash"] = ".sh",
            ["sh"] = ".sh",
            ["python"] = ".py"
        };

        internal static string GetScriptArgumentsFormat(string scriptType)
        {
            if (_defaultArguments.TryGetValue(scriptType, out var argFormat))
            {
                return argFormat;
            }
            return "";
        }

        internal static string GetScriptFileExtension(string scriptType)
        {
            if (_extensions.TryGetValue(scriptType, out var extension))
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
                throw new ArgumentException($"Failed to parse COMMAND [..ARGS] from {shellOption}");
            }
        }

        internal static string GetDefaultShellNameForScript(string path, Common.Tracing trace, string prependPath)
        {
            switch (Path.GetExtension(path))
            {
                case ".sh":
                    // use 'sh' args but prefer bash
                    if (WhichUtil.Which("bash", false, trace, prependPath) != null)
                    {
                        return "bash";
                    }
                    return "sh";
                case ".ps1":
                    if (WhichUtil.Which("pwsh", false, trace, prependPath) != null)
                    {
                        return "pwsh";
                    }
                    return "powershell";
                default:
                    throw new ArgumentException($"{path} is not a valid path to a script. Make sure it ends in '.sh' or '.ps1'.");
            }
        }
    }
}
