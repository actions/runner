using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GitHub.Runner.Sdk
{
    public sealed class NetRcCredential(string login, string password)
    {
        public string Login { get; } = login;

        public string Password { get; } = password;
    }

    /// <summary>
    /// Reads explicit machine credentials for action archive redirect hosts.
    /// Default entries are ignored so redirects cannot send a fallback password
    /// to an unlisted host. Machine names are hostnames without ports.
    /// </summary>
    public static class NetRcUtil
    {
        /// <summary>
        /// Resolves the credential file: the NETRC environment variable wins,
        /// otherwise ~/.netrc (falling back to ~/_netrc, the spelling some
        /// Windows tools use). Returns an explicit NETRC path even if it does
        /// not exist, so the reader can report a configuration error.
        /// </summary>
        public static string ResolveFilePath()
        {
            var netrcEnv = Environment.GetEnvironmentVariable("NETRC");
            if (!string.IsNullOrEmpty(netrcEnv))
            {
                return netrcEnv;
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
            {
                return null;
            }

            foreach (var fileName in new[] { ".netrc", "_netrc" })
            {
                var candidate = Path.Combine(home, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        public static IReadOnlyDictionary<string, NetRcCredential> ReadCredentials(string filePath, Action<string> warning = null)
        {
            var machines = new Dictionary<string, NetRcCredential>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(filePath))
            {
                return machines;
            }

            try
            {
                using var reader = File.OpenText(filePath);
                string currentMachine = null;
                string login = null;
                string password = null;

                void FlushEntry()
                {
                    if (!string.IsNullOrEmpty(currentMachine) && password != null)
                    {
                        // First matching entry wins, as with other .netrc consumers.
                        machines.TryAdd(currentMachine, new NetRcCredential(login ?? string.Empty, password));
                    }

                    currentMachine = null;
                    login = null;
                    password = null;
                }

                string token;
                while ((token = ReadToken(reader, skipComments: true)) != null)
                {
                    switch (token.ToLowerInvariant())
                    {
                        case "machine":
                            FlushEntry();
                            currentMachine = ReadToken(reader);
                            break;
                        case "default":
                            // End the previous machine entry without accepting fallback credentials.
                            FlushEntry();
                            break;
                        case "login":
                            login = ReadToken(reader);
                            break;
                        case "password":
                            password = ReadToken(reader);
                            break;
                        case "account":
                            // Recognized but unused; consume the value.
                            ReadToken(reader);
                            break;
                        case "macdef":
                            // Skip the macro name and body through the next blank line.
                            reader.ReadLine();
                            string line;
                            while ((line = reader.ReadLine()) != null && !string.IsNullOrWhiteSpace(line))
                            {
                            }
                            break;
                    }
                }

                FlushEntry();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException or NotSupportedException)
            {
                // An unreadable or malformed file must not supply partial credentials.
                machines.Clear();
                // Exception messages can contain input from the credential file. Report only a safe reason.
                var reason = ex switch
                {
                    FileNotFoundException or DirectoryNotFoundException => "file does not exist",
                    UnauthorizedAccessException => "access was denied",
                    FormatException => "invalid netrc syntax",
                    ArgumentException or NotSupportedException => "invalid file path",
                    _ => "an I/O error occurred"
                };
                warning?.Invoke($"Could not read netrc file '{filePath}': {reason}. Continuing without netrc credentials.");
            }

            return machines;
        }

        // Double-quoted values follow curl 7.84.0+ escape rules (\n, \r, \t, \", \\).
        // Python's netrc parser and git-credential-netrc use different escape rules.
        // Skip comments only between directives; a value beginning with '#' is literal.
        private static string ReadToken(TextReader reader, bool skipComments = false)
        {
            int next;
            while ((next = reader.Peek()) != -1)
            {
                if (char.IsWhiteSpace((char)next))
                {
                    reader.Read();
                }
                else if (skipComments && next == '#')
                {
                    reader.ReadLine();
                }
                else
                {
                    break;
                }
            }

            if (next == -1)
            {
                return null;
            }

            bool quoted = next == '"';
            if (quoted)
            {
                reader.Read();
            }
            var token = new StringBuilder();
            while ((next = reader.Peek()) != -1)
            {
                if (!quoted && char.IsWhiteSpace((char)next))
                {
                    return token.ToString();
                }

                reader.Read();
                if (quoted && next == '"')
                {
                    return token.ToString();
                }

                if (quoted && (next == '\r' || next == '\n'))
                {
                    break;
                }

                if (quoted && next == '\\')
                {
                    next = reader.Read();
                    if (next == -1 || next == '\r' || next == '\n')
                    {
                        break;
                    }
                    token.Append(next switch
                    {
                        'n' => '\n',
                        'r' => '\r',
                        't' => '\t',
                        _ => (char)next
                    });
                }
                else
                {
                    token.Append((char)next);
                }
            }

            if (quoted)
            {
                throw new FormatException("Unterminated quoted value in netrc.");
            }
            return token.ToString();
        }
    }
}
