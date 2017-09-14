using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Services.Agent
{
    public sealed class Command
    {
        private const string LoggingCommandPrefix = "##vso[";
        private static readonly EscapeMapping[] s_escapeMappings = new[]
        {
            // TODO: What about %?
            new EscapeMapping(token: ";", replacement: "%3B"),
            new EscapeMapping(token: "\r", replacement: "%0D"),
            new EscapeMapping(token: "\n", replacement: "%0A"),
            new EscapeMapping(token: "]", replacement: "%5D"),
        };
        private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Command(string area, string eventName)
        {
            ArgUtil.NotNullOrEmpty(area, nameof(area));
            ArgUtil.NotNullOrEmpty(eventName, nameof(eventName));
            Area = area;
            Event = eventName;
        }

        public string Area { get; }

        public string Event { get; }

        public Dictionary<string, string> Properties => _properties;

        public string Data { get; set; }

        public static bool TryParse(string message, out Command command)
        {
            command = null;
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            try
            {
                // Get the index of the prefix.
                int prefixIndex = message.IndexOf(LoggingCommandPrefix);
                if (prefixIndex < 0)
                {
                    return false;
                }

                // Get the index of the separator between the command info and the data.
                int rbIndex = message.IndexOf(']', prefixIndex);
                if (rbIndex < 0)
                {
                    return false;
                }

                // Get the command info (area.event and properties).
                int cmdIndex = prefixIndex + LoggingCommandPrefix.Length;
                string cmdInfo = message.Substring(cmdIndex, rbIndex - cmdIndex);

                // Get the command name (area.event).
                int spaceIndex = cmdInfo.IndexOf(' ');
                string commandName = 
                    spaceIndex < 0
                    ? cmdInfo
                    : cmdInfo.Substring(0, spaceIndex);

                // Get the area and event.
                string[] areaEvent = commandName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (areaEvent.Length != 2)
                {
                    return false;
                }

                string areaName = areaEvent[0];
                string eventName = areaEvent[1];

                // Initialize the command.
                command = new Command(areaName, eventName);

                // Set the properties.
                if (spaceIndex > 0)
                {
                    string propertiesStr = cmdInfo.Substring(spaceIndex + 1);
                    string[] splitProperties = propertiesStr.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string propertyStr in splitProperties)
                    {
                        string[] pair = propertyStr.Split(new[] { '=' }, count: 2, options: StringSplitOptions.RemoveEmptyEntries);
                        if (pair.Length == 2)
                        {
                            command.Properties[pair[0]] = Unescape(pair[1]);
                        }
                    }
                }

                command.Data = Unescape(message.Substring(rbIndex + 1));
                return true;
            }
            catch
            {
                command = null;
                return false;
            }
        }

        private static string Unescape(string escaped)
        {
            if (string.IsNullOrEmpty(escaped))
            {
                return string.Empty;
            }

            string unescaped = escaped;
            foreach (EscapeMapping mapping in s_escapeMappings)
            {
                unescaped = unescaped.Replace(mapping.Replacement, mapping.Token);
            }

            return unescaped;
        }

        private sealed class EscapeMapping
        {
            public string Replacement { get; }
            public string Token { get; }

            public EscapeMapping(string token, string replacement)
            {
                ArgUtil.NotNullOrEmpty(token, nameof(token));
                ArgUtil.NotNullOrEmpty(replacement, nameof(replacement));
                Token = token;
                Replacement = replacement;
            }
        }
    }
}
