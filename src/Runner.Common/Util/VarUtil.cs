using System;
using System.Collections.Generic;
using System.Linq;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Common.Util
{
    public static class VarUtil
    {
        public static StringComparer EnvironmentVariableKeyComparer
        {
            get
            {
                switch (Constants.Runner.Platform)
                {
                    case Constants.OSPlatform.Linux:
                    case Constants.OSPlatform.OSX:
                        return StringComparer.Ordinal;
                    case Constants.OSPlatform.Windows:
                        return StringComparer.OrdinalIgnoreCase;
                    default:
                        throw new NotSupportedException(); // Should never reach here.
                }
            }
        }

        public static string OS
        {
            get
            {
                switch (Constants.Runner.Platform)
                {
                    case Constants.OSPlatform.Linux:
                        return "Linux";
                    case Constants.OSPlatform.OSX:
                        return "Darwin";
                    case Constants.OSPlatform.Windows:
                        return Environment.GetEnvironmentVariable("OS");
                    default:
                        throw new NotSupportedException(); // Should never reach here.
                }
            }
        }

        public static string OSArchitecture
        {
            get
            {
                switch (Constants.Runner.PlatformArchitecture)
                {
                    case Constants.Architecture.X86:
                        return "X86";
                    case Constants.Architecture.X64:
                        return "X64";
                    case Constants.Architecture.Arm:
                        return "ARM";
                    default:
                        throw new NotSupportedException(); // Should never reach here.
                }
            }
        }

        public static void ExpandValues(IHostContext context, IDictionary<string, string> source, IDictionary<string, string> target)
        {
            ArgUtil.NotNull(context, nameof(context));
            ArgUtil.NotNull(source, nameof(source));
            Tracing trace = context.GetTrace(nameof(VarUtil));
            trace.Entering();
            target = target ?? new Dictionary<string, string>();

            // This algorithm does not perform recursive replacement.

            // Process each key in the target dictionary.
            foreach (string targetKey in target.Keys.ToArray())
            {
                trace.Verbose($"Processing expansion for: '{targetKey}'");
                int startIndex = 0;
                int prefixIndex;
                int suffixIndex;
                string targetValue = target[targetKey] ?? string.Empty;

                // Find the next macro within the target value.
                while (startIndex < targetValue.Length &&
                    (prefixIndex = targetValue.IndexOf(Constants.Variables.MacroPrefix, startIndex, StringComparison.Ordinal)) >= 0 &&
                    (suffixIndex = targetValue.IndexOf(Constants.Variables.MacroSuffix, prefixIndex + Constants.Variables.MacroPrefix.Length, StringComparison.Ordinal)) >= 0)
                {
                    // A candidate was found.
                    string variableKey = targetValue.Substring(
                        startIndex: prefixIndex + Constants.Variables.MacroPrefix.Length,
                        length: suffixIndex - prefixIndex - Constants.Variables.MacroPrefix.Length);
                    trace.Verbose($"Found macro candidate: '{variableKey}'");
                    string variableValue;
                    if (!string.IsNullOrEmpty(variableKey) &&
                        TryGetValue(trace, source, variableKey, out variableValue))
                    {
                        // A matching variable was found.
                        // Update the target value.
                        trace.Verbose("Macro found.");
                        targetValue = string.Concat(
                            targetValue.Substring(0, prefixIndex),
                            variableValue ?? string.Empty,
                            targetValue.Substring(suffixIndex + Constants.Variables.MacroSuffix.Length));

                        // Bump the start index to prevent recursive replacement.
                        startIndex = prefixIndex + (variableValue ?? string.Empty).Length;
                    }
                    else
                    {
                        // A matching variable was not found.
                        trace.Verbose("Macro not found.");
                        startIndex = prefixIndex + 1;
                    }
                }

                target[targetKey] = targetValue ?? string.Empty;
            }
        }

        private static bool TryGetValue(Tracing trace, IDictionary<string, string> source, string name, out string val)
        {
            if (source.TryGetValue(name, out val))
            {
                val = val ?? string.Empty;
                trace.Verbose($"Get '{name}': '{val}'");
                return true;
            }

            val = null;
            trace.Verbose($"Get '{name}' (not found)");
            return false;
        }
    }
}
