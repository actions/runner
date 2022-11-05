using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace Runner.Client {
    public static class Util {
        public static List<string> ReadEnvFile(string filePath) {
            var ret = new List<string>();
            Action<string, string> SetEnvironmentVariable = (name, value) => {
                ret.Add($"{name}={value}");
            };
            ReadEnvFile(filePath, SetEnvironmentVariable);
            return ret;
        }
        public static void ReadEnvFile(string filePath, Action<string, string> SetEnvironmentVariable) {
            var text = File.ReadAllText(filePath) ?? string.Empty;
            if(filePath.EndsWith(".yml") || filePath.EndsWith(".yaml")) {
                var des = new DeserializerBuilder().Build();
                foreach(var kv in des.Deserialize<IDictionary<string, string>>(text)) {
                    SetEnvironmentVariable(kv.Key, kv.Value);
                }
                return;
            }
            var index = 0;
            var line = ReadLine(text, ref index);
            while (line != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    var equalsIndex = line.IndexOf("=", StringComparison.Ordinal);
                    var heredocIndex = line.IndexOf("<<", StringComparison.Ordinal);

                    // Normal style NAME=VALUE
                    if (equalsIndex >= 0 && (heredocIndex < 0 || equalsIndex < heredocIndex))
                    {
                        var split = line.Split(new[] { '=' }, 2, StringSplitOptions.None);
                        if (string.IsNullOrEmpty(line))
                        {
                            throw new Exception($"Invalid environment variable format '{line}'. Environment variable name must not be empty");
                        }
                        SetEnvironmentVariable(split[0], split[1]);
                    }
                    // Heredoc style NAME<<EOF
                    else if (heredocIndex >= 0 && (equalsIndex < 0 || heredocIndex < equalsIndex))
                    {
                        var split = line.Split(new[] { "<<" }, 2, StringSplitOptions.None);
                        if (string.IsNullOrEmpty(split[0]) || string.IsNullOrEmpty(split[1]))
                        {
                            throw new Exception($"Invalid environment variable format '{line}'. Environment variable name must not be empty and delimiter must not be empty");
                        }
                        var name = split[0];
                        var delimiter = split[1];
                        var startIndex = index; // Start index of the value (inclusive)
                        var endIndex = index;   // End index of the value (exclusive)
                        var tempLine = ReadLine(text, ref index, out var newline);
                        while (!string.Equals(tempLine, delimiter, StringComparison.Ordinal))
                        {
                            if (tempLine == null)
                            {
                                throw new Exception($"Invalid environment variable value. Matching delimiter not found '{delimiter}'");
                            }
                            endIndex = index - newline.Length;
                            tempLine = ReadLine(text, ref index, out newline);
                        }

                        var value = endIndex > startIndex ? text.Substring(startIndex, endIndex - startIndex) : string.Empty;
                        SetEnvironmentVariable(name, value);
                    }
                    else
                    {
                        throw new Exception($"Invalid environment variable format '{line}'");
                    }
                }

                line = ReadLine(text, ref index);
            }
        }
        private static string ReadLine(
            string text,
            ref int index)
        {
            return ReadLine(text, ref index, out _);
        }

        private static string ReadLine(
            string text,
            ref int index,
            out string newline)
        {
            if (index >= text.Length)
            {
                newline = null;
                return null;
            }

            var originalIndex = index;
            var lfIndex = text.IndexOf("\n", index, StringComparison.Ordinal);
            if (lfIndex < 0)
            {
                index = text.Length;
                newline = null;
                return text.Substring(originalIndex);
            }

            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) {
                var crLFIndex = text.IndexOf("\r\n", index, StringComparison.Ordinal);
                if (crLFIndex >= 0 && crLFIndex < lfIndex)
                {
                    index = crLFIndex + 2; // Skip over CRLF
                    newline = "\r\n";
                    return text.Substring(originalIndex, crLFIndex - originalIndex);
                }
            }

            index = lfIndex + 1; // Skip over LF
            newline = "\n";
            return text.Substring(originalIndex, lfIndex - originalIndex);
        }

        public static string[] SafeConcatArray(this string[] left, string[] right) {
            return right == null ? left : left?.Concat(right)?.ToArray() ?? right;
        }
    }
}
