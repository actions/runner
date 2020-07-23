using GitHub.Runner.Listener.Configuration;
using GitHub.Runner.Common.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GitHub.DistributedTask.Logging;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Listener
{
    public sealed class CommandSettings
    {
        private readonly Dictionary<string, string> _envArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly CommandLineParser _parser;
        private readonly IPromptManager _promptManager;
        private readonly Tracing _trace;

        private readonly string[] validCommands =
        {
            Constants.Runner.CommandLine.Commands.Configure,
            Constants.Runner.CommandLine.Commands.Remove,
            Constants.Runner.CommandLine.Commands.Run,
            Constants.Runner.CommandLine.Commands.Warmup,
        };

        private readonly string[] validFlags =
        {
            Constants.Runner.CommandLine.Flags.Commit,
            Constants.Runner.CommandLine.Flags.Help,
            Constants.Runner.CommandLine.Flags.Replace,
            Constants.Runner.CommandLine.Flags.RunAsService,
            Constants.Runner.CommandLine.Flags.Once,
            Constants.Runner.CommandLine.Flags.Unattended,
            Constants.Runner.CommandLine.Flags.Version
        };

        private readonly string[] validArgs =
        {
            Constants.Runner.CommandLine.Args.Auth,
            Constants.Runner.CommandLine.Args.Labels,
            Constants.Runner.CommandLine.Args.MonitorSocketAddress,
            Constants.Runner.CommandLine.Args.Name,
            Constants.Runner.CommandLine.Args.RunnerGroup,
            Constants.Runner.CommandLine.Args.StartupType,
            Constants.Runner.CommandLine.Args.Token,
            Constants.Runner.CommandLine.Args.Url,
            Constants.Runner.CommandLine.Args.UserName,
            Constants.Runner.CommandLine.Args.WindowsLogonAccount,
            Constants.Runner.CommandLine.Args.WindowsLogonPassword,
            Constants.Runner.CommandLine.Args.Work
        };

        // Commands.
        public bool Configure => TestCommand(Constants.Runner.CommandLine.Commands.Configure);
        public bool Remove => TestCommand(Constants.Runner.CommandLine.Commands.Remove);
        public bool Run => TestCommand(Constants.Runner.CommandLine.Commands.Run);
        public bool Warmup => TestCommand(Constants.Runner.CommandLine.Commands.Warmup);

        // Flags.
        public bool Commit => TestFlag(Constants.Runner.CommandLine.Flags.Commit);
        public bool Help => TestFlag(Constants.Runner.CommandLine.Flags.Help);
        public bool Unattended => TestFlag(Constants.Runner.CommandLine.Flags.Unattended);
        public bool Version => TestFlag(Constants.Runner.CommandLine.Flags.Version);

        public bool RunOnce => TestFlag(Constants.Runner.CommandLine.Flags.Once);

        // Constructor.
        public CommandSettings(IHostContext context, string[] args)
        {
            ArgUtil.NotNull(context, nameof(context));
            _promptManager = context.GetService<IPromptManager>();
            _trace = context.GetTrace(nameof(CommandSettings));

            // Parse the command line args.
            _parser = new CommandLineParser(
                hostContext: context,
                secretArgNames: Constants.Runner.CommandLine.Args.Secrets);
            _parser.Parse(args);

            // Store and remove any args passed via environment variables.
            IDictionary environment = Environment.GetEnvironmentVariables();
            string envPrefix = "ACTIONS_RUNNER_INPUT_";
            foreach (DictionaryEntry entry in environment)
            {
                // Test if starts with ACTIONS_RUNNER_INPUT_.
                string fullKey = entry.Key as string ?? string.Empty;
                if (fullKey.StartsWith(envPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string val = (entry.Value as string ?? string.Empty).Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        // Extract the name.
                        string name = fullKey.Substring(envPrefix.Length);

                        // Mask secrets.
                        bool secret = Constants.Runner.CommandLine.Args.Secrets.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
                        if (secret)
                        {
                            context.SecretMasker.AddValue(val);
                        }

                        // Store the value.
                        _envArgs[name] = val;
                    }

                    // Remove from the environment block.
                    _trace.Info($"Removing env var: '{fullKey}'");
                    Environment.SetEnvironmentVariable(fullKey, null);
                }
            }
        }

        // Validate commandline parser result
        public List<string> Validate()
        {
            List<string> unknowns = new List<string>();

            // detect unknown commands
            unknowns.AddRange(_parser.Commands.Where(x => !validCommands.Contains(x, StringComparer.OrdinalIgnoreCase)));

            // detect unknown flags
            unknowns.AddRange(_parser.Flags.Where(x => !validFlags.Contains(x, StringComparer.OrdinalIgnoreCase)));

            // detect unknown args
            unknowns.AddRange(_parser.Args.Keys.Where(x => !validArgs.Contains(x, StringComparer.OrdinalIgnoreCase)));

            return unknowns;
        }

        //
        // Interactive flags.
        //
        public bool GetReplace()
        {
            return TestFlagOrPrompt(
                name: Constants.Runner.CommandLine.Flags.Replace,
                description: "Would you like to replace the existing runner? (Y/N)",
                defaultValue: false);
        }

        public bool GetRunAsService()
        {
            return TestFlagOrPrompt(
                name: Constants.Runner.CommandLine.Flags.RunAsService,
                description: "Would you like to run the runner as service? (Y/N)",
                defaultValue: false);
        }

        //
        // Args.
        //
        public string GetAuth(string defaultValue)
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.Auth,
                description: "How would you like to authenticate?",
                defaultValue: defaultValue,
                validator: Validators.AuthSchemeValidator);
        }

        public string GetRunnerName()
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.Name,
                description: "Enter the name of runner:",
                defaultValue: Environment.MachineName ?? "myrunner",
                validator: Validators.NonEmptyValidator);
        }

        public string GetRunnerGroupName(string defaultPoolName = null)
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.RunnerGroup,
                description: "Enter the name of the runner group to add this runner to:",
                defaultValue: defaultPoolName ?? "default",
                validator: Validators.NonEmptyValidator);
        }

        public string GetToken()
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.Token,
                description: "What is your pool admin oauth access token?",
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator);
        }

        public string GetRunnerRegisterToken()
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.Token,
                description: "What is your runner register token?",
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator);
        }

        public string GetRunnerDeletionToken()
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.Token,
                description: "Enter runner remove token:",
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator);
        }

        public string GetUrl(bool suppressPromptIfEmpty = false)
        {
            // Note, GetArg does not consume the arg (like GetArgOrPrompt does).
            if (suppressPromptIfEmpty &&
                string.IsNullOrEmpty(GetArg(Constants.Runner.CommandLine.Args.Url)))
            {
                return string.Empty;
            }

            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.Url,
                description: "What is the URL of your repository?",
                defaultValue: string.Empty,
                validator: Validators.ServerUrlValidator);
        }

        public string GetWindowsLogonAccount(string defaultValue, string descriptionMsg)
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.WindowsLogonAccount,
                description: descriptionMsg,
                defaultValue: defaultValue,
                validator: Validators.NTAccountValidator);
        }

        public string GetWindowsLogonPassword(string accountName)
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.WindowsLogonPassword,
                description: $"Password for the account {accountName}",
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator);
        }

        public string GetWork()
        {
            return GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.Work,
                description: "Enter name of work folder:",
                defaultValue: Constants.Path.WorkDirectory,
                validator: Validators.NonEmptyValidator);
        }

        public string GetMonitorSocketAddress()
        {
            return GetArg(Constants.Runner.CommandLine.Args.MonitorSocketAddress);
        }

        // This is used to find out the source from where the Runner.Listener.exe was launched at the time of run
        public string GetStartupType()
        {
            return GetArg(Constants.Runner.CommandLine.Args.StartupType);
        }

        public ISet<string> GetLabels()
        {
            var labelSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string labels = GetArgOrPrompt(
                name: Constants.Runner.CommandLine.Args.Labels,
                description: $"This runner will have the following labels: 'self-hosted', '{VarUtil.OS}', '{VarUtil.OSArchitecture}' \nEnter any additional labels (ex. label-1,label-2):",
                defaultValue: string.Empty,
                validator: Validators.LabelsValidator,
                isOptional: true);

            if (!string.IsNullOrEmpty(labels))
            {
                labelSet = labels.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            return labelSet;
        }

        //
        // Private helpers.
        //
        private string GetArg(string name)
        {
            string result;
            if (!_parser.Args.TryGetValue(name, out result))
            {
                result = GetEnvArg(name);
            }

            return result;
        }

        private void RemoveArg(string name)
        {
            if (_parser.Args.ContainsKey(name))
            {
                _parser.Args.Remove(name);
            }

            if (_envArgs.ContainsKey(name))
            {
                _envArgs.Remove(name);
            }
        }

        private string GetArgOrPrompt(
            string name,
            string description,
            string defaultValue,
            Func<string, bool> validator,
            bool isOptional = false)
        {
            // Check for the arg in the command line parser.
            ArgUtil.NotNull(validator, nameof(validator));
            string result = GetArg(name);

            // Return the arg if it is not empty and is valid.
            _trace.Info($"Arg '{name}': '{result}'");
            if (!string.IsNullOrEmpty(result))
            {
                // After read the arg from input commandline args, remove it from Arg dictionary,
                // This will help if bad arg value passed through CommandLine arg, when ConfigurationManager ask CommandSetting the second time,
                // It will prompt for input instead of continue use the bad input.
                _trace.Info($"Remove {name} from Arg dictionary.");
                RemoveArg(name);

                if (validator(result))
                {
                    return result;
                }

                _trace.Info("Arg is invalid.");
            }

            // Otherwise prompt for the arg.
            return _promptManager.ReadValue(
                argName: name,
                description: description,
                secret: Constants.Runner.CommandLine.Args.Secrets.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)),
                defaultValue: defaultValue,
                validator: validator,
                unattended: Unattended,
                isOptional: isOptional);
        }

        private string GetEnvArg(string name)
        {
            string val;
            if (_envArgs.TryGetValue(name, out val) && !string.IsNullOrEmpty(val))
            {
                _trace.Info($"Env arg '{name}': '{val}'");
                return val;
            }

            return null;
        }

        private bool TestCommand(string name)
        {
            bool result = _parser.IsCommand(name);
            _trace.Info($"Command '{name}': '{result}'");
            return result;
        }

        private bool TestFlag(string name)
        {
            bool result = _parser.Flags.Contains(name);
            if (!result)
            {
                string envStr = GetEnvArg(name);
                if (!bool.TryParse(envStr, out result))
                {
                    result = false;
                }
            }

            _trace.Info($"Flag '{name}': '{result}'");
            return result;
        }

        private bool TestFlagOrPrompt(
            string name,
            string description,
            bool defaultValue)
        {
            bool result = TestFlag(name);
            if (!result)
            {
                result = _promptManager.ReadBool(
                    argName: name,
                    description: description,
                    defaultValue: defaultValue,
                    unattended: Unattended);
            }

            return result;
        }
    }
}
