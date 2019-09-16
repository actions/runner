﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GitHub.Runner.Worker.Container
{
    public class DockerUtil
    {
        public static List<PortMapping> ParseDockerPort(IList<string> portMappingLines)
        {
            const string targetPort = "targetPort";
            const string proto = "proto";
            const string host = "host";
            const string hostPort = "hostPort";

            //"TARGET_PORT/PROTO -> HOST:HOST_PORT"
            string pattern = $"^(?<{targetPort}>\\d+)/(?<{proto}>\\w+) -> (?<{host}>.+):(?<{hostPort}>\\d+)$";

            List<PortMapping> portMappings = new List<PortMapping>();
            foreach(var line in portMappingLines)
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
    }
}
