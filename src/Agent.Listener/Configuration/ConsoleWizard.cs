using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Configuration
{
    [ServiceLocator(Default = typeof(ConsoleWizard))]
    public interface IConsoleWizard
    {
        String GetConfigurationValue(
            IHostContext context,
            string configName,
            Dictionary<String, ArgumentMetaData> configMetaData,
            Dictionary<String, String> args,
            Boolean UnAttended);
    }

    public class ConsoleWizard : IConsoleWizard
    {
        public string GetConfigurationValue(
            IHostContext context,
            string configName,
            Dictionary<String, ArgumentMetaData> configMetaData,
            Dictionary<String, String> args,
            Boolean IsUnattended)
        {
            // If the value for the parameter is specified in the command line it will take precedence.
            // If its not found in the cmdline parameter, it will try to read from existing config
            // and prompt from user quoting the default value read from the config.
            // Error out otherwise, we can't find the value for the parameter
            String value = string.Empty;

            TraceSource m_trace = context.Trace["ConsoleWizard"];
            String description = configMetaData[configName].Description;
            bool isSecret = (bool)configMetaData[configName].IsSercret;
            String defaultValue = configMetaData[configName].DefaultValue;
            Func<String, bool> validator = configMetaData[configName].Validator;

            // TODO: Put this in resource files
            var errorMessage = string.Format("Enter an valid value for {0}", description);

            if (args != null && args.ContainsKey(configName))
            {
                value = args[configName];
                m_trace.Info("Value for the parameter {0} found from the command line", configName);

                m_trace.Info("Validating the value for the parameter {0}", configName);
                if (validator == null || (!string.IsNullOrEmpty(value) && validator(value)))
                {
                    return value;
                }
            }

            if (IsUnattended)
            {
                Console.WriteLine("Invalid configuration provided for {0}. Terminating unattended configuration", configName);
                Environment.Exit(1);
            }

            while (true)
            {
                value = ReadParameterFromUser(configName, isSecret, description, defaultValue);

                if (!string.IsNullOrEmpty(value))
                {
                    if (validator == null || (!string.IsNullOrEmpty(value) && validator(value)))
                    {
                        break;
                    }
                }

                m_trace.Info("Invalid value for the parameter {0}", configName);
                Console.WriteLine(errorMessage);
            }

            return value;
        }


        private string ReadParameterFromUser(string parameterName, bool isSecret, string description, string defaultValue)
        {
            // TODO: Localize?
            var defaultValueFormat = string.Format("(Default {0} is {1})", description, defaultValue);
            var prompt = string.Format("Enter {0} {1} > ", description, string.IsNullOrEmpty(defaultValue) ? string.Empty : defaultValueFormat);

            Console.Write(prompt);

            if (isSecret)
            {
                return ReadSecret();
            }

            var inputValue = Console.ReadLine().Trim();

            if (string.IsNullOrEmpty(inputValue))
            {
                return defaultValue;
            }

            return inputValue;
        }

        private static string ReadSecret()
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

            return new String(chars.ToArray());
        }
    }
}