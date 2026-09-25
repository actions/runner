
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GitHub.Runner.Sdk;
using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;

namespace GitHub.Runner.Worker.Handlers
{
    internal static class ScriptHandlerHelpers
    {
        private static readonly Dictionary<string, string> _defaultArguments = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cmd"] = "/D /E:ON /V:OFF /S /C \"CALL \"{0}\"\"",
            ["pwsh"] = "-command \". '{0}'\"",
            ["powershell"] = "-command \". '{0}'\"",
            ["bash"] = "--noprofile --norc -e -o pipefail {0}",
            ["sh"] = "-e {0}",
            ["python"] = "{0}"
        };

        private static readonly Dictionary<string, string> _extensions = new(StringComparer.OrdinalIgnoreCase)
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
                case "bash":
                case "sh":
                    contents = FixBashEnvironmentVariables(contents);
                    break;
            }
            return contents;
        }

        /// <summary>
        /// Fixes unquoted environment variables in bash/sh scripts to prevent issues with paths containing spaces.
        /// This method quotes environment variables used in shell redirects and command substitutions.
        /// </summary>
        /// <param name="contents">The shell script content to fix</param>
        /// <returns>Fixed shell script content with properly quoted environment variables</returns>
        private static string FixBashEnvironmentVariables(string contents)
        {
            if (string.IsNullOrEmpty(contents))
            {
                return contents;
            }

            // Pattern to match environment variables in shell redirects that aren't already quoted
            // This targets patterns like: >> $GITHUB_STEP_SUMMARY, > $GITHUB_OUTPUT, etc.
            // but avoids already quoted ones like: >> "$GITHUB_STEP_SUMMARY" or >> '$GITHUB_OUTPUT'
            var redirectPattern = new Regex(
                @"(\s+(?:>>|>|<|2>>|2>)\s+)(\$[A-Za-z_][A-Za-z0-9_]*)\b(?!\s*['""])",
                RegexOptions.Compiled | RegexOptions.Multiline
            );

            // Replace unquoted environment variables in redirects with quoted versions
            contents = redirectPattern.Replace(contents, match =>
            {
                var redirectOperator = match.Groups[1].Value; // e.g., " >> "
                var envVar = match.Groups[2].Value; // e.g., "$GITHUB_STEP_SUMMARY"
                
                return $"{redirectOperator}\"{envVar}\"";
            });

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
    }
}
