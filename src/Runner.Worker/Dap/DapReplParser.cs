using System;
using System.Collections.Generic;
using System.Text;

namespace GitHub.Runner.Worker.Dap
{
    /// <summary>
    /// Base type for all REPL DSL commands.
    /// </summary>
    internal abstract class DapReplCommand
    {
    }

    /// <summary>
    /// <c>help</c> or <c>help("run")</c>
    /// </summary>
    internal sealed class HelpCommand : DapReplCommand
    {
        public string Topic { get; set; }
    }

    /// <summary>
    /// <c>run("echo hello")</c> or
    /// <c>run("echo hello", shell: "bash", env: { FOO: "bar" }, working_directory: "/tmp")</c>
    /// </summary>
    internal sealed class RunCommand : DapReplCommand
    {
        public string Script { get; set; }
        public string Shell { get; set; }
        public Dictionary<string, string> Env { get; set; }
        public string WorkingDirectory { get; set; }
    }

    /// <summary>
    /// Parses REPL input into typed <see cref="DapReplCommand"/> objects.
    ///
    /// Grammar (intentionally minimal — extend as the DSL grows):
    /// <code>
    ///   help                                → HelpCommand { Topic = null }
    ///   help("run")                         → HelpCommand { Topic = "run" }
    ///   run("script body")                  → RunCommand  { Script = "script body" }
    ///   run("script", shell: "bash")        → RunCommand  { Shell = "bash" }
    ///   run("script", env: { K: "V" })      → RunCommand  { Env = { K → V } }
    ///   run("script", working_directory: "p")→ RunCommand  { WorkingDirectory = "p" }
    /// </code>
    ///
    /// Parsing is intentionally hand-rolled rather than regex-based so it can
    /// handle nested braces, quoted strings with escapes, and grow to support
    /// future commands without accumulating regex complexity.
    /// </summary>
    internal static class DapReplParser
    {
        /// <summary>
        /// Attempts to parse REPL input into a command. Returns null if the
        /// input does not match any known DSL command (i.e. it should be
        /// treated as an expression instead).
        /// </summary>
        internal static DapReplCommand TryParse(string input, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var trimmed = input.Trim();

            // help / help("topic")
            if (trimmed.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("help(", StringComparison.OrdinalIgnoreCase))
            {
                return ParseHelp(trimmed, out error);
            }

            // run("...")
            if (trimmed.StartsWith("run(", StringComparison.OrdinalIgnoreCase))
            {
                return ParseRun(trimmed, out error);
            }

            // Not a DSL command
            return null;
        }

        internal static string GetGeneralHelp()
        {
            return """
                Actions Debug Console

                Commands:
                  help                  Show this help
                  help("run")           Show help for the run command
                  run("script")         Execute a script (like a workflow run step)

                Anything else is evaluated as a GitHub Actions expression.
                  Example: github.repository
                  Example: ${{ github.event_name }}

                """;
        }

        internal static string GetRunHelp()
        {
            return """
                run command — execute a script in the job context

                Usage:
                  run("echo hello")
                  run("echo $FOO", shell: "bash")
                  run("echo $FOO", env: { FOO: "bar" })
                  run("ls", working_directory: "/tmp")
                  run("echo $X", shell: "bash", env: { X: "1" }, working_directory: "/tmp")

                Options:
                  shell:             Shell to use (default: job default, e.g. bash)
                  env:               Extra environment variables as { KEY: "value" }
                  working_directory:  Working directory for the command

                Behavior:
                  - Equivalent to a workflow `run:` step
                  - Expressions in the script body are expanded (${{ ... }})
                  - Output is streamed in real time and secrets are masked

                """;
        }

        #region Parsers

        private static HelpCommand ParseHelp(string input, out string error)
        {
            error = null;
            if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                return new HelpCommand();
            }

            // help("topic")
            var inner = ExtractParenthesizedArgs(input, "help", out error);
            if (error != null) return null;

            var topic = ExtractQuotedString(inner.Trim(), out error);
            if (error != null) return null;

            return new HelpCommand { Topic = topic };
        }

        private static RunCommand ParseRun(string input, out string error)
        {
            error = null;

            var inner = ExtractParenthesizedArgs(input, "run", out error);
            if (error != null) return null;

            // Split into argument list respecting quotes and braces
            var args = SplitArguments(inner, out error);
            if (error != null) return null;
            if (args.Count == 0)
            {
                error = "run() requires a script argument. Example: run(\"echo hello\")";
                return null;
            }

            // First arg must be the script body (a quoted string)
            var script = ExtractQuotedString(args[0].Trim(), out error);
            if (error != null)
            {
                error = $"First argument to run() must be a quoted string. {error}";
                return null;
            }

            var cmd = new RunCommand { Script = script };

            // Parse remaining keyword arguments
            for (int i = 1; i < args.Count; i++)
            {
                var kv = args[i].Trim();
                var colonIdx = kv.IndexOf(':');
                if (colonIdx <= 0)
                {
                    error = $"Expected keyword argument (e.g. shell: \"bash\"), got: {kv}";
                    return null;
                }

                var key = kv.Substring(0, colonIdx).Trim();
                var value = kv.Substring(colonIdx + 1).Trim();

                switch (key.ToLowerInvariant())
                {
                    case "shell":
                        cmd.Shell = ExtractQuotedString(value, out error);
                        if (error != null) { error = $"shell: {error}"; return null; }
                        break;

                    case "working_directory":
                        cmd.WorkingDirectory = ExtractQuotedString(value, out error);
                        if (error != null) { error = $"working_directory: {error}"; return null; }
                        break;

                    case "env":
                        cmd.Env = ParseEnvBlock(value, out error);
                        if (error != null) { error = $"env: {error}"; return null; }
                        break;

                    default:
                        error = $"Unknown option: {key}. Valid options: shell, env, working_directory";
                        return null;
                }
            }

            return cmd;
        }

        #endregion

        #region Low-level parsing helpers

        /// <summary>
        /// Given "cmd(...)" returns the inner content between the outer parens.
        /// </summary>
        private static string ExtractParenthesizedArgs(string input, string prefix, out string error)
        {
            error = null;
            var start = prefix.Length; // skip "cmd"
            if (start >= input.Length || input[start] != '(')
            {
                error = $"Expected '(' after {prefix}";
                return null;
            }

            if (input[input.Length - 1] != ')')
            {
                error = $"Expected ')' at end of {prefix}(...)";
                return null;
            }

            return input.Substring(start + 1, input.Length - start - 2);
        }

        /// <summary>
        /// Extracts a double-quoted string value, handling escaped quotes.
        /// </summary>
        internal static string ExtractQuotedString(string input, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(input))
            {
                error = "Expected a quoted string, got empty input";
                return null;
            }

            if (input[0] != '"')
            {
                error = $"Expected a quoted string starting with \", got: {Truncate(input, 40)}";
                return null;
            }

            var sb = new StringBuilder();
            for (int i = 1; i < input.Length; i++)
            {
                if (input[i] == '\\' && i + 1 < input.Length)
                {
                    sb.Append(input[i + 1]);
                    i++;
                }
                else if (input[i] == '"')
                {
                    // Check nothing meaningful follows the closing quote
                    var rest = input.Substring(i + 1).Trim();
                    if (rest.Length > 0)
                    {
                        error = $"Unexpected content after closing quote: {Truncate(rest, 40)}";
                        return null;
                    }
                    return sb.ToString();
                }
                else
                {
                    sb.Append(input[i]);
                }
            }

            error = "Unterminated string (missing closing \")";
            return null;
        }

        /// <summary>
        /// Splits a comma-separated argument list, respecting quoted strings
        /// and nested braces so that <c>"a, b", env: { K: "V, W" }</c> is
        /// correctly split into two arguments.
        /// </summary>
        internal static List<string> SplitArguments(string input, out string error)
        {
            error = null;
            var result = new List<string>();
            var current = new StringBuilder();
            int depth = 0;
            bool inQuote = false;

            for (int i = 0; i < input.Length; i++)
            {
                var ch = input[i];

                if (ch == '\\' && inQuote && i + 1 < input.Length)
                {
                    current.Append(ch);
                    current.Append(input[++i]);
                    continue;
                }

                if (ch == '"')
                {
                    inQuote = !inQuote;
                    current.Append(ch);
                    continue;
                }

                if (!inQuote)
                {
                    if (ch == '{')
                    {
                        depth++;
                        current.Append(ch);
                        continue;
                    }
                    if (ch == '}')
                    {
                        depth--;
                        current.Append(ch);
                        continue;
                    }
                    if (ch == ',' && depth == 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                        continue;
                    }
                }

                current.Append(ch);
            }

            if (inQuote)
            {
                error = "Unterminated string in arguments";
                return null;
            }
            if (depth != 0)
            {
                error = "Unmatched braces in arguments";
                return null;
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result;
        }

        /// <summary>
        /// Parses <c>{ KEY: "value", KEY2: "value2" }</c> into a dictionary.
        /// </summary>
        internal static Dictionary<string, string> ParseEnvBlock(string input, out string error)
        {
            error = null;
            var trimmed = input.Trim();
            if (!trimmed.StartsWith("{") || !trimmed.EndsWith("}"))
            {
                error = "Expected env block in the form { KEY: \"value\" }";
                return null;
            }

            var inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
            if (string.IsNullOrEmpty(inner))
            {
                return new Dictionary<string, string>();
            }

            var pairs = SplitArguments(inner, out error);
            if (error != null) return null;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in pairs)
            {
                var colonIdx = pair.IndexOf(':');
                if (colonIdx <= 0)
                {
                    error = $"Expected KEY: \"value\" pair, got: {Truncate(pair.Trim(), 40)}";
                    return null;
                }

                var key = pair.Substring(0, colonIdx).Trim();
                var val = ExtractQuotedString(pair.Substring(colonIdx + 1).Trim(), out error);
                if (error != null) return null;

                result[key] = val;
            }

            return result;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (value == null) return "(null)";
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
        }

        #endregion
    }
}
