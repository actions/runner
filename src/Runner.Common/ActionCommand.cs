using GitHub.Runner.Sdk;
using GitHub.Runner.Common.Util;
using System;
using System.Collections.Generic;

namespace GitHub.Runner.Common
{
    public sealed class ActionCommand
    {
        private static readonly StringEscapingUtil.EscapeMapping[] _escapeMappings = new[]
        {
            new StringEscapingUtil.EscapeMapping(token: ";", replacement: "%3B"),
            new StringEscapingUtil.EscapeMapping(token: "\r", replacement: "%0D"),
            new StringEscapingUtil.EscapeMapping(token: "\n", replacement: "%0A"),
            new StringEscapingUtil.EscapeMapping(token: "]", replacement: "%5D"),
            new StringEscapingUtil.EscapeMapping(token: "%", replacement: "%25"),
        };

        private static readonly StringEscapingUtil.EscapeMapping[] _escapeDataMappings = new[]
        {
            new StringEscapingUtil.EscapeMapping(token: "\r", replacement: "%0D"),
            new StringEscapingUtil.EscapeMapping(token: "\n", replacement: "%0A"),
        };

        private static readonly StringEscapingUtil.EscapeMapping[] _escapePropertyMappings = new[]
        {
            new StringEscapingUtil.EscapeMapping(token: "\r", replacement: "%0D"),
            new StringEscapingUtil.EscapeMapping(token: "\n", replacement: "%0A"),
            new StringEscapingUtil.EscapeMapping(token: ":", replacement: "%3A"),
            new StringEscapingUtil.EscapeMapping(token: ",", replacement: "%2C"),
            new StringEscapingUtil.EscapeMapping(token: "%", replacement: "%25"),
        };

        private readonly Dictionary<string, string> _properties = new(StringComparer.OrdinalIgnoreCase);
        public const string Prefix = "##[";
        public const string _commandKey = "::";

        public ActionCommand(string command)
        {
            ArgUtil.NotNullOrEmpty(command, nameof(command));
            Command = command;
        }

        public string Command { get; }


        public Dictionary<string, string> Properties => _properties;

        public string Data { get; set; }

        public static bool TryParseV2(string message, HashSet<string> registeredCommands, out ActionCommand command)
        {
            command = null;
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            try
            {
                // the message needs to start with the keyword after trim leading space.
                message = message.TrimStart();
                if (!message.StartsWith(_commandKey))
                {
                    return false;
                }

                // Get the index of the separator between the command info and the data.
                int endIndex = message.IndexOf(_commandKey, _commandKey.Length);
                if (endIndex < 0)
                {
                    return false;
                }

                // Get the command info (command and properties).
                int cmdIndex = _commandKey.Length;
                string cmdInfo = message.Substring(cmdIndex, endIndex - cmdIndex);

                // Get the command name
                int spaceIndex = cmdInfo.IndexOf(' ');
                string commandName =
                    spaceIndex < 0
                    ? cmdInfo
                    : cmdInfo.Substring(0, spaceIndex);

                if (registeredCommands.Contains(commandName))
                {
                    // Initialize the command.
                    command = new ActionCommand(commandName);
                }
                else
                {
                    return false;
                }

                // Set the properties.
                if (spaceIndex > 0)
                {
                    string propertiesStr = cmdInfo.Substring(spaceIndex + 1).Trim();
                    string[] splitProperties = propertiesStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string propertyStr in splitProperties)
                    {
                        string[] pair = propertyStr.Split(new[] { '=' }, count: 2, options: StringSplitOptions.RemoveEmptyEntries);
                        if (pair.Length == 2)
                        {
                            command.Properties[pair[0]] = StringEscapingUtil.UnescapeString(pair[1], _escapePropertyMappings);
                        }
                    }
                }

                command.Data = StringEscapingUtil.UnescapeString(message.Substring(endIndex + _commandKey.Length), _escapeDataMappings);
                return true;
            }
            catch
            {
                command = null;
                return false;
            }
        }

        public static bool TryParse(string message, HashSet<string> registeredCommands, out ActionCommand command)
        {
            command = null;
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            try
            {
                // Get the index of the prefix.
                int prefixIndex = message.IndexOf(Prefix);
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

                // Get the command info (command and properties).
                int cmdIndex = prefixIndex + Prefix.Length;
                string cmdInfo = message.Substring(cmdIndex, rbIndex - cmdIndex);

                // Get the command name
                int spaceIndex = cmdInfo.IndexOf(' ');
                string commandName =
                    spaceIndex < 0
                    ? cmdInfo
                    : cmdInfo.Substring(0, spaceIndex);

                if (registeredCommands.Contains(commandName))
                {
                    // Initialize the command.
                    command = new ActionCommand(commandName);
                }
                else
                {
                    return false;
                }

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
                            command.Properties[pair[0]] = StringEscapingUtil.UnescapeString(pair[1], _escapeMappings);
                        }
                    }
                }

                command.Data = StringEscapingUtil.UnescapeString(message.Substring(rbIndex + 1), _escapeMappings);
                return true;
            }
            catch
            {
                command = null;
                return false;
            }
        }
    }
}
