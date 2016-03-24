using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
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
        private ITerminal _terminal;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);
            _terminal = HostContext.GetService<ITerminal>();
        }

        public bool ReadBool(
            string argName,
            string description,
            bool defaultValue,
            Dictionary<String, String> args,
            bool unattended)
        {
            string def = defaultValue ? StringUtil.Loc("Y") : StringUtil.Loc("N");
            string answer = ReadValue(argName, description, false, def, Validators.BoolValidator, args, unattended);
            
            return String.Equals(answer, StringUtil.Loc("Y"), StringComparison.CurrentCultureIgnoreCase) ||
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
            Trace.Info(nameof(ReadValue));

            // If the value for the parameter is specified in the command line it will take precedence.
            // If its not found in the cmdline parameter, it will try to read from existing config
            // and prompt from user quoting the default value read from the config.
            // Error out otherwise, we can't find the value for the parameter
            string value = string.Empty;

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
                _terminal.WriteLine(StringUtil.Loc("InvalidConfigFor0TerminatingUnattended", argName));
                // TODO: Don't Environment.Exit(). Makes unit testing difficult.
                System.Environment.Exit(1);
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
                _terminal.WriteLine(StringUtil.Loc("EnterValidValueFor0", description));
            }

            return value;
        }


        private string ReadParameterFromUser(string parameterName, bool secret, string description, string defaultValue)
        {
            Trace.Info(nameof(ReadParameterFromUser));
            string prompt =
                string.IsNullOrEmpty(defaultValue)
                ? StringUtil.Loc("Prompt0", description)
                : StringUtil.Loc("Prompt0Default1", description, defaultValue);
            _terminal.Write($"{prompt} > ");

            if (secret)
            {
                Trace.Info("Getting secret");
                return _terminal.ReadSecret()?.Trim() ?? string.Empty;
            }

            string inputValue = _terminal.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(inputValue))
            {
                return defaultValue;
            }

            return inputValue;
        }
    }
}