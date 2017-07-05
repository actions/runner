using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    public sealed class CommandSettings
    {
        private readonly IHostContext _context;
        private readonly Dictionary<string, string> _envArgs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly CommandLineParser _parser;
        private readonly IPromptManager _promptManager;
        private ISecretMasker _secretMasker;
        private readonly Tracing _trace;

        private readonly string[] validCommands =
        {
            Constants.Agent.CommandLine.Commands.Configure,
            Constants.Agent.CommandLine.Commands.Run,
            Constants.Agent.CommandLine.Commands.Unconfigure
        };

        private readonly string[] validFlags =
        {
            Constants.Agent.CommandLine.Flags.AcceptTeeEula,
            Constants.Agent.CommandLine.Flags.AddMachineGroupTags,
            Constants.Agent.CommandLine.Flags.AddDeploymentGroupTags,
            Constants.Agent.CommandLine.Flags.Commit,
            Constants.Agent.CommandLine.Flags.DeploymentGroup,
            Constants.Agent.CommandLine.Flags.DisableScreenSaver,
            Constants.Agent.CommandLine.Flags.EnableAutoLogon,
            Constants.Agent.CommandLine.Flags.Help,
            Constants.Agent.CommandLine.Flags.MachineGroup,
            Constants.Agent.CommandLine.Flags.Replace,
            Constants.Agent.CommandLine.Flags.RunAsService,
            Constants.Agent.CommandLine.Flags.RestartNow,
            Constants.Agent.CommandLine.Flags.Unattended,
            Constants.Agent.CommandLine.Flags.Version
        };

        private readonly string[] validArgs =
        {
            Constants.Agent.CommandLine.Args.Agent,
            Constants.Agent.CommandLine.Args.Auth,
            Constants.Agent.CommandLine.Args.CollectionName,
            Constants.Agent.CommandLine.Args.DeploymentGroupName,
            Constants.Agent.CommandLine.Args.DeploymentGroupTags,
            Constants.Agent.CommandLine.Args.MachineGroupName,
            Constants.Agent.CommandLine.Args.MachineGroupTags,
            Constants.Agent.CommandLine.Args.NotificationPipeName,
            Constants.Agent.CommandLine.Args.Password,
            Constants.Agent.CommandLine.Args.Pool,
            Constants.Agent.CommandLine.Args.ProjectName,
            Constants.Agent.CommandLine.Args.StartupType,
            Constants.Agent.CommandLine.Args.Token,
            Constants.Agent.CommandLine.Args.Url,
            Constants.Agent.CommandLine.Args.UserName,
            Constants.Agent.CommandLine.Args.WindowsLogonAccount,
            Constants.Agent.CommandLine.Args.WindowsLogonPassword,
            Constants.Agent.CommandLine.Args.Work
        };

        // Commands.
        public bool Configure => TestCommand(Constants.Agent.CommandLine.Commands.Configure);
        public bool Run => TestCommand(Constants.Agent.CommandLine.Commands.Run);
        public bool Unconfigure => TestCommand(Constants.Agent.CommandLine.Commands.Unconfigure);

        // Flags.
        public bool Commit => TestFlag(Constants.Agent.CommandLine.Flags.Commit);
        public bool Help => TestFlag(Constants.Agent.CommandLine.Flags.Help);
        public bool Unattended => TestFlag(Constants.Agent.CommandLine.Flags.Unattended);
        public bool Version => TestFlag(Constants.Agent.CommandLine.Flags.Version);
        public bool DeploymentGroup => TestFlag(Constants.Agent.CommandLine.Flags.MachineGroup) || TestFlag(Constants.Agent.CommandLine.Flags.DeploymentGroup);

        // Constructor.
        public CommandSettings(IHostContext context, string[] args)
        {
            ArgUtil.NotNull(context, nameof(context));
            _context = context;
            _promptManager = context.GetService<IPromptManager>();
            _secretMasker = context.GetService<ISecretMasker>();
            _trace = context.GetTrace(nameof(CommandSettings));

            // Parse the command line args.
            _parser = new CommandLineParser(
                hostContext: context,
                secretArgNames: Constants.Agent.CommandLine.Args.Secrets);
            _parser.Parse(args);

            // Store and remove any args passed via environment variables.
            IDictionary environment = Environment.GetEnvironmentVariables();
            string envPrefix = "VSTS_AGENT_INPUT_";
            foreach (DictionaryEntry entry in environment)
            {
                // Test if starts with VSTS_AGENT_INPUT_.
                string fullKey = entry.Key as string ?? string.Empty;
                if (fullKey.StartsWith(envPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string val = (entry.Value as string ?? string.Empty).Trim();
                    if (!string.IsNullOrEmpty(val))
                    {
                        // Extract the name.
                        string name = fullKey.Substring(envPrefix.Length);

                        // Mask secrets.
                        bool secret = Constants.Agent.CommandLine.Args.Secrets.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
                        if (secret)
                        {
                            _secretMasker.AddValue(val);
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
            unknowns.AddRange(_parser.Commands.Where(x => !validCommands.Contains(x)));

            // detect unknown flags
            unknowns.AddRange(_parser.Flags.Where(x => !validFlags.Contains(x)));

            // detect unknown args
            unknowns.AddRange(_parser.Args.Keys.Where(x => !validArgs.Contains(x)));

            return unknowns;
        }

        //
        // Interactive flags.
        //
        public bool GetAcceptTeeEula()
        {
            return TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.AcceptTeeEula,
                description: StringUtil.Loc("AcceptTeeEula"),
                defaultValue: false);
        }

        public bool GetReplace()
        {
            return TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.Replace,
                description: StringUtil.Loc("Replace"),
                defaultValue: false);
        }

        public bool GetRunAsService()
        {
            return TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.RunAsService,
                description: StringUtil.Loc("RunAgentAsServiceDescription"),
                defaultValue: false);
        }

        public bool GetEnableAutoLogon()
        {
            return TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.EnableAutoLogon,
                description: StringUtil.Loc("EnableAutoLogon"),
                defaultValue: false);
        }

        public bool GetDisableScreenSaver()
        {
            return TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.DisableScreenSaver,
                description: StringUtil.Loc("DisableScreenSaver"),
                defaultValue: true);
        }

        public bool GetRestartNow()
        {
            return TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.RestartNow,
                description: StringUtil.Loc("RestartNow"),
                defaultValue: true);
        }

        public bool GetDeploymentGroupTagsRequired()
        {
            return TestFlag(Constants.Agent.CommandLine.Flags.AddMachineGroupTags)
                   || TestFlagOrPrompt(
                           name: Constants.Agent.CommandLine.Flags.AddDeploymentGroupTags,
                           description: StringUtil.Loc("AddDeploymentGroupTagsFlagDescription"),
                           defaultValue: false);
        }

        //
        // Args.
        //
        public string GetAgentName()
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Agent,
                description: StringUtil.Loc("AgentName"),
                defaultValue: Environment.MachineName ?? "myagent",
                validator: Validators.NonEmptyValidator);
        }

        public string GetAuth(string defaultValue)
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Auth,
                description: StringUtil.Loc("AuthenticationType"),
                defaultValue: defaultValue,
                validator: Validators.AuthSchemeValidator);
        }

        public string GetPassword()
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Password,
                description: StringUtil.Loc("Password"),
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator);
        }

        public string GetPool()
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Pool,
                description: StringUtil.Loc("AgentMachinePoolNameLabel"),
                defaultValue: "default",
                validator: Validators.NonEmptyValidator);
        }

        public string GetToken()
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Token,
                description: StringUtil.Loc("PersonalAccessToken"),
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator);
        }

        public string GetUrl()
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Url,
                description: StringUtil.Loc("ServerUrl"),
                defaultValue: string.Empty,
                validator: Validators.ServerUrlValidator);
        }

        public string GetDeploymentGroupName()
        {
            var result = GetArg(Constants.Agent.CommandLine.Args.MachineGroupName);
            if (string.IsNullOrEmpty(result))
            {
                return GetArgOrPrompt(
                            name: Constants.Agent.CommandLine.Args.DeploymentGroupName,
                            description: StringUtil.Loc("DeploymentGroupName"),
                            defaultValue: string.Empty,
                            validator: Validators.NonEmptyValidator);
            }
            return result;
        }

        public string GetProjectName(string defaultValue)
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.ProjectName,
                description: StringUtil.Loc("ProjectName"),
                defaultValue: defaultValue,
                validator: Validators.NonEmptyValidator);
        }

        public string GetCollectionName()
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.CollectionName,
                description: StringUtil.Loc("CollectionName"),
                defaultValue: "DefaultCollection",
                validator: Validators.NonEmptyValidator);
        }

        public string GetDeploymentGroupTags()
        {
            var result = GetArg(Constants.Agent.CommandLine.Args.MachineGroupTags);
            if (string.IsNullOrEmpty(result))
            {
                return GetArgOrPrompt(
                    name: Constants.Agent.CommandLine.Args.DeploymentGroupTags,
                    description: StringUtil.Loc("DeploymentGroupTags"),
                    defaultValue: string.Empty,
                    validator: Validators.NonEmptyValidator);
            }
            return result;
        }

        public string GetUserName()
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.UserName,
                description: StringUtil.Loc("UserName"),
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator);
        }

        public string GetWindowsLogonAccount(string defaultValue, string descriptionMsg)
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.WindowsLogonAccount,
                description: descriptionMsg,
                defaultValue: defaultValue,
                validator: Validators.NTAccountValidator);
        }

        public string GetWindowsLogonPassword(string accountName)
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.WindowsLogonPassword,
                description: StringUtil.Loc("WindowsLogonPasswordDescription", accountName),
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator);
        }

        public string GetWork()
        {
            return GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Work,
                description: StringUtil.Loc("WorkFolderDescription"),
                defaultValue: Constants.Path.WorkDirectory,
                validator: Validators.NonEmptyValidator);
        }

        public string GetNotificationPipeName()
        {
            return GetArg(Constants.Agent.CommandLine.Args.NotificationPipeName);
        }

        public string GetNotificationSocketAddress()
        {
            return GetArg(Constants.Agent.CommandLine.Args.NotificationSocketAddress);
        }

        ///This is used to find out the source from where the agent.listner.exe was launched at the time of run
        public string GetStartupType()
        {
            return GetArg(Constants.Agent.CommandLine.Args.StartupType);
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
            Func<string, bool> validator)
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
                // It will prompt for input intead of continue use the bad input.
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
                secret: Constants.Agent.CommandLine.Args.Secrets.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)),
                defaultValue: defaultValue,
                validator: validator,
                unattended: Unattended);
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
