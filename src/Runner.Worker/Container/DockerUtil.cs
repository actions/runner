using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitHub.Runner.Worker.Container
{
    public class DockerUtil
    {
        private static readonly Regex QuoteEscape = new Regex(@"(\\*)" + "\"", RegexOptions.Compiled);
        private static readonly Regex EndOfStringEscape = new Regex(@"(\\+)$", RegexOptions.Compiled);

        public static List<PortMapping> ParseDockerPort(IList<string> portMappingLines)
        {
            const string targetPort = "targetPort";
            const string proto = "proto";
            const string host = "host";
            const string hostPort = "hostPort";

            //"TARGET_PORT/PROTO -> HOST:HOST_PORT"
            string pattern = $"^(?<{targetPort}>\\d+)/(?<{proto}>\\w+) -> (?<{host}>.+):(?<{hostPort}>\\d+)$";

            List<PortMapping> portMappings = new List<PortMapping>();
            foreach (var line in portMappingLines)
            {
                Match m = Regex.Match(line, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
                if (m.Success)
                {
                    portMappings.Add(new PortMapping(
                        m.Groups[hostPort].Value,
                        m.Groups[targetPort].Value,
                        m.Groups[proto].Value
                    ));
                }
            }
            return portMappings;
        }

        public static string ParsePathFromConfigEnv(IList<string> configEnvLines)
        {
            // Config format is VAR=value per line
            foreach (var line in configEnvLines)
            {
                var keyValue = line.Split("=", 2);
                if (keyValue.Length == 2 && string.Equals(keyValue[0], "PATH"))
                {
                    return keyValue[1];
                }
            }
            return "";
        }

        public static string ParseRegistryHostnameFromImageName(string name)
        {
            var nameSplit = name.Split('/');
            // Single slash is implictly from Dockerhub, unless first part has .tld or :port
            if (nameSplit.Length == 2 && (nameSplit[0].Contains(":") || nameSplit[0].Contains(".")))
            {
                return nameSplit[0];
            }
            // All other non Dockerhub registries
            else if (nameSplit.Length > 2)
            {
                return nameSplit[0];
            }
            return "";
        }

        public static string CreateEscapedOption(string flag, string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                return "";
            }
            return $"{flag} {EscapeString(key)}";
        }

        public static string CreateEscapedOption(string flag, string key, string value)
        {
            if (String.IsNullOrEmpty(key))
            {
                return "";
            }
            var escapedString = EscapeString($"{key}={value}");
            return $"{flag} {escapedString}";
        }

        private static string EscapeString(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return "";
            }
            // Dotnet escaping rules are weird here, we can only escape \ if it precedes a "
            // If a double quotation mark follows two or an even number of backslashes, each proceeding backslash pair is replaced with one backslash and the double quotation mark is removed.
            // If a double quotation mark follows an odd number of backslashes, including just one, each preceding pair is replaced with one backslash and the remaining backslash is removed; however, in this case the double quotation mark is not removed.
            // https://docs.microsoft.com/en-us/dotnet/api/system.environment.getcommandlineargs?redirectedfrom=MSDN&view=net-6.0#remarks

            // First, find any \ followed by a " and double the number of \ + 1.
             value = QuoteEscape.Replace(value, @"$1$1\" + "\"");
            // Next, what if it ends in `\`, it would escape the end quote. So, we need to detect that at the end of the string and perform the same escape
            // Luckily, we can just use the $ character with detects the end of string in regex
            value = EndOfStringEscape.Replace(value, @"$1$1");
            // Finally, wrap it in quotes
            return $"\"{value}\"";
        }
    }
}
