using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Services.Agent.Listener.Configuration
{
    [ServiceLocator(Default = typeof(PromptManager))]
    public interface IPromptManager: IAgentService
    {
        bool ReadBool(
            string argName,
            string description,
            bool defaultValue,
            bool unattended);

        string ReadValue(
            string argName,
            string description,
            bool secret,
            string defaultValue,
            Func<String, bool> validator,
            bool unattended);
    }

    public sealed class PromptManager : AgentService, IPromptManager
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
            bool unattended)
        {
            string answer = ReadValue(
                argName: argName,
                description: description,
                secret: false,
                defaultValue: defaultValue ? StringUtil.Loc("Y") : StringUtil.Loc("N"),
                validator: Validators.BoolValidator,
                unattended: unattended);
            return String.Equals(answer, "true", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(answer, StringUtil.Loc("Y"), StringComparison.CurrentCultureIgnoreCase);
        }

        public string ReadValue(
            string argName,
            string description,
            bool secret,
            string defaultValue,
            Func<string, bool> validator,
            bool unattended)
        {
            Trace.Info(nameof(ReadValue));
            ArgUtil.NotNull(validator, nameof(validator));
            string value = string.Empty;

            // Check if unattended.
            if (unattended)
            {
                // Return the default value if specified.
                if (!string.IsNullOrEmpty(defaultValue))
                {
                    return defaultValue;
                }

                // Otherwise throw.
                throw new Exception(StringUtil.Loc("InvalidConfigFor0TerminatingUnattended", argName));
            }

            // Prompt until a valid value is read.
            while (true)
            {
                // Write the message prompt.
                string prompt =
                    string.IsNullOrEmpty(defaultValue)
                    ? StringUtil.Loc("Prompt0", description)
                    : StringUtil.Loc("Prompt0Default1", description, defaultValue);
                _terminal.Write($"{prompt} > ");

                // Read and trim the value.
                value = secret ? _terminal.ReadSecret() : _terminal.ReadLine();
                value = value?.Trim() ?? string.Empty;

                // Return the default if not specified.
                if (string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(defaultValue))
                {
                    Trace.Info($"Falling back to the default: '{defaultValue}'");
                    return defaultValue;
                }

                // Return the value if it is not empty and it is valid.
                // Otherwise try the loop again.
                if (!string.IsNullOrEmpty(value))
                {
                    if (validator(value))
                    {
                        return value;
                    }
                    else
                    {
                        Trace.Info("Invalid value.");
                        _terminal.WriteLine(StringUtil.Loc("EnterValidValueFor0", description));
                    }
                }
            }
        }
    }
}