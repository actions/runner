using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public interface ICommandExtension : IExtension
    {
        string CommandArea { get; }

        void ProcessCommand(IExecutionContext context, Command command);
    }

    public class Command
    {
        private Dictionary<string, string> _properties;
        private const string _loggingCommandPrefix = "##vso";

        private static string[] _escapeCharactersToken = new string[] { ";", "\r", "\n" };
        private static string[] _escapeCharactersReplacement = new string[] { "%3B", "%0D", "%0A" };

        public Command(string area, string eventName)
        {
            Area = area;
            Event = eventName;
        }

        public string Area
        {
            get;
            private set;
        }

        public string Event
        {
            get;
            private set;
        }

        public Dictionary<string, string> Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                return _properties;
            }
        }

        public string Data
        {
            get;
            set;
        }

        // TODO: Simplify the logic.
        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0}[{1}.{2}", _loggingCommandPrefix, this.Area, this.Event);

            Boolean splitSpace = false;
            foreach (var parameter in this.Properties)
            {
                String value = parameter.Value;
                if (!String.IsNullOrEmpty(value))
                {
                    for (int index = 0; index < _escapeCharactersToken.Length; index++)
                    {
                        value = value.Replace(_escapeCharactersToken[index], _escapeCharactersReplacement[index]);
                    }

                    if (!splitSpace)
                    {
                        sb.AppendFormat(CultureInfo.InvariantCulture, " ");
                        splitSpace = true;
                    }

                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1};", parameter.Key, value);
                }
            }

            String data = this.Data;
            if (!String.IsNullOrEmpty(data))
            {
                for (int index = 0; index < _escapeCharactersToken.Length; index++)
                {
                    data = data.Replace(_escapeCharactersToken[index], _escapeCharactersReplacement[index]);
                }
            }

            return String.Format("{0}]{1}", sb.ToString(), data);
        }

        // TODO: Simplify the logic.
        public static Boolean TryParse(String message, out Command command)
        {
            command = null;
            if (String.IsNullOrEmpty(message))
            {
                return false;
            }

            try
            {
                int prePos = message.IndexOf(_loggingCommandPrefix);
                if (prePos == -1)
                {
                    return false;
                }

                message = message.Substring(prePos);

                int lbPos = message.IndexOf('[');
                int rbPos = message.IndexOf(']');

                if (lbPos == -1 || rbPos == -1 || rbPos - lbPos < 3)
                {
                    return false;
                }

                String prefix = message.Substring(0, lbPos);
                if (!String.Equals(prefix, _loggingCommandPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                var cmdInfo = message.Substring(lbPos + 1, rbPos - lbPos - 1);
                int spPos = cmdInfo.IndexOf(' ');
                String commandInfo;
                if (spPos == -1)
                {
                    commandInfo = cmdInfo;
                }
                else
                {
                    commandInfo = cmdInfo.Substring(0, spPos);
                }

                String[] areaEvent = commandInfo.Split(new Char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (areaEvent.Length != 2)
                {
                    return false;
                }

                String areaName = areaEvent[0];
                String eventName = areaEvent[1];
                command = new Command(areaName, eventName);

                if (spPos != -1)
                {
                    String properties = cmdInfo.Substring(spPos + 1);
                    String[] propLines = properties.Split(new Char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var prop in propLines)
                    {
                        String[] pair = prop.Split(new Char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);

                        if (pair.Length == 2)
                        {
                            for (int index = 0; index < _escapeCharactersReplacement.Length; index++)
                            {
                                pair[1] = pair[1].Replace(_escapeCharactersReplacement[index], _escapeCharactersToken[index]);
                            }

                            command.Properties[pair[0]] = pair[1];
                        }
                    }
                }

                command.Data = message.Substring(rbPos + 1);

                if (!String.IsNullOrEmpty(command.Data))
                {
                    for (int index = 0; index < _escapeCharactersReplacement.Length; index++)
                    {
                        command.Data = command.Data.Replace(_escapeCharactersReplacement[index], _escapeCharactersToken[index]);
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }
    }
}
