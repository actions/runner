using GitHub.Runner.Common;
using GitHub.Runner.Common.Util;
using GitHub.Runner.Sdk;
using System;

namespace GitHub.Runner.Listener.Configuration
{
    [ServiceLocator(Default = typeof(PromptManager))]
    public interface IPromptManager : IRunnerService
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
            bool unattended,
            bool isOptional = false);
    }

    public sealed class PromptManager : RunnerService, IPromptManager
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
                defaultValue: defaultValue ? "Y" : "N",
                validator: Validators.BoolValidator,
                unattended: unattended);
            return String.Equals(answer, "true", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(answer, "Y", StringComparison.CurrentCultureIgnoreCase);
        }

        public string ReadValue(
            string argName,
            string description,
            bool secret,
            string defaultValue,
            Func<string, bool> validator,
            bool unattended,
            bool isOptional = false)
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
                else if (isOptional) 
                {
                    return string.Empty;
                }

                // Otherwise throw.
                throw new Exception($"Invalid configuration provided for {argName}. Terminating unattended configuration.");
            }

            // Prompt until a valid value is read.
            while (true)
            {
                // Write the message prompt.
                _terminal.Write($"{description} ", ConsoleColor.White);

                if(!string.IsNullOrEmpty(defaultValue))
                {
                    _terminal.Write($"[press Enter for {defaultValue}] ");
                }
                else if (isOptional){
                    _terminal.Write($"[press Enter to skip] ");
                }

                // Read and trim the value.
                value = secret ? _terminal.ReadSecret() : _terminal.ReadLine();
                value = value?.Trim() ?? string.Empty;

                // Return the default if not specified.
                if (string.IsNullOrEmpty(value))
                {
                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        Trace.Info($"Falling back to the default: '{defaultValue}'");
                        return defaultValue;
                    }
                    else if (isOptional)
                    {
                        return string.Empty;
                    }
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
                        _terminal.WriteLine("Entered value is invalid", ConsoleColor.Yellow);
                    }
                }
            }
        }
    }
}
