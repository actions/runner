using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    [ServiceLocator(Default = typeof(ConsoleWizard))]
    public interface IConsoleWizard: IAgentService
    {
        bool ReadBool(
            string argName,
            string description,
            bool defaultValue,
            Dictionary<String, String> args,
            bool unattended);

        string ReadValue(
            string argName,
            string description,
            bool secret,
            string defaultValue,
            Func<String, bool> validator,
            Dictionary<String, String> args,
            bool unattended);
    }

    public class ConsoleWizard : AgentService, IConsoleWizard
    {
        public bool ReadBool(
            string argName,
            string description,
            bool defaultValue,
            Dictionary<String, String> args,
            bool unattended)
        {
            string def = defaultValue ? "Y" : "N";
            string answer = ReadValue(argName, description, false, def, Validators.BoolValidator, args, unattended);
            
            return String.Equals(answer, "Y", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(answer, "true", StringComparison.OrdinalIgnoreCase);
        }

        public string ReadValue(
            string argName,
            string description,
            bool secret,
            string defaultValue,
            Func<String, bool> validator,
            Dictionary<String, String> args,
            bool unattended)
        {
            Trace.Info("ReadValue()");

            // If the value for the parameter is specified in the command line it will take precedence.
            // If its not found in the cmdline parameter, it will try to read from existing config
            // and prompt from user quoting the default value read from the config.
            // Error out otherwise, we can't find the value for the parameter
            String value = string.Empty;

            // TODO: Put this in resource files
            var errorMessage = string.Format("Enter a valid value for {0}", description);

            if (args != null && args.ContainsKey(argName))
            {
                Trace.Info("args contained {0}", argName);
                value = args[argName];
                Trace.Info("Value for the parameter {0} found from the command line", argName);

                Trace.Info("Validating the value for the parameter {0}", argName);
                if (validator == null || (!string.IsNullOrEmpty(value) && validator(value)))
                {
                    return value;
                }
            }

            if (unattended)
            {
                Console.WriteLine("Invalid configuration provided for {0}. Terminating unattended configuration", argName);
                Environment.Exit(1);
            }

            while (true)
            {
                value = ReadParameterFromUser(argName, secret, description, defaultValue);
                Trace.Verbose(value);

                if (!string.IsNullOrEmpty(value))
                {
                    Trace.Info("Validating: {0} with {1}", value, validator);
                    if (validator == null || validator(value))
                    {
                        break;
                    }
                }

                Trace.Info("Invalid value for the parameter {0}", argName);
                Console.WriteLine(errorMessage);
            }

            return value;
        }


        private string ReadParameterFromUser(string parameterName, bool secret, string description, string defaultValue)
        {
            Trace.Info("ReadParameterFromUser()");
            // TODO: Localize?
            var defaultValueFormat = string.Format("(enter for {0})", defaultValue);
            var prompt = string.Format("Enter {0} {1} > ", description, string.IsNullOrEmpty(defaultValue) ? string.Empty : defaultValueFormat);

            Console.Write(prompt);

            if (secret)
            {
                Trace.Info("Getting secret");
                return ReadSecret();
            }

            var inputValue = Console.ReadLine().Trim();

            if (string.IsNullOrEmpty(inputValue))
            {
                return defaultValue;
            }

            return inputValue;
        }

        private string ReadSecret()
        {
            List<char> chars = new List<char>();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (chars.Count > 0)
                    {
                        chars.RemoveAt(chars.Count - 1);
                    }
                }
                else if (key.KeyChar > 0)
                {
                    chars.Add(key.KeyChar);
                    Console.Write("*");
                }
            }

            string val = new String(chars.ToArray()).Trim();
            Trace.Info("Secret gathered. {0} chars", val);
            return val;
        }
    }
}