using Microsoft.VisualStudio.Services.Agent.Listener.Configuration;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Listener
{
    public sealed class CommandSettings
    {
        private readonly IHostContext _context;
        private readonly CommandLineParser _parser;
        private readonly IPromptManager _promptManager;
        private readonly Tracing _trace;

        // Commands.
        public bool Configure => TestCommand(Constants.Agent.CommandLine.Commands.Configure);
        public bool Run => TestCommand(Constants.Agent.CommandLine.Commands.Run);
        public bool Unconfigure => TestCommand(Constants.Agent.CommandLine.Commands.Unconfigure);

        // Flags.
        public bool Commit => TestFlag(Constants.Agent.CommandLine.Flags.Commit);
        public bool Help => TestFlag(Constants.Agent.CommandLine.Flags.Help);
        public bool NoStart => TestFlag(Constants.Agent.CommandLine.Flags.NoStart);
        public bool Unattended => TestFlag(Constants.Agent.CommandLine.Flags.Unattended);
        public bool Version => TestFlag(Constants.Agent.CommandLine.Flags.Version);

        // Constructor.
        public CommandSettings(IHostContext context, string[] args)
        {
            ArgUtil.NotNull(context, nameof(context));
            _context = context;
            _promptManager = context.GetService<IPromptManager>();
            _trace = context.GetTrace(nameof(CommandSettings));

            // Parse the command line args.
            _parser = new CommandLineParser(
                hostContext: context,
                secretArgNames: Constants.Agent.CommandLine.Args.Secrets);
            _parser.Parse(args);
        }

        //
        // Interactive flags.
        //
        public async Task<bool> GetAcceptTeeEula(CancellationToken token)
        {
            return await TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.AcceptTeeEula,
                description: StringUtil.Loc("AcceptTeeEula"),
                defaultValue: false,
                token: token);
        }

        public async Task<bool> GetReplace(CancellationToken token)
        {
            return await TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.Replace,
                description: StringUtil.Loc("Replace"),
                defaultValue: false,
                token: token);
        }

        public async Task<bool> GetRunAsService(CancellationToken token)
        {
            return await TestFlagOrPrompt(
                name: Constants.Agent.CommandLine.Flags.RunAsService,
                description: StringUtil.Loc("RunAgentAsServiceDescription"),
                defaultValue: false,
                token: token);
        }

        //
        // Args.
        //
        public async Task<string> GetAgentName(CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Agent,
                description: StringUtil.Loc("AgentName"),
                defaultValue: Environment.MachineName ?? "myagent",
                validator: Validators.NonEmptyValidator,
                token: token);
        }

        public async Task<string> GetAuth(string defaultValue, CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Auth,
                description: StringUtil.Loc("AuthenticationType"),
                defaultValue: defaultValue,
                validator: Validators.AuthSchemeValidator,
                token: token);
        }

        public async Task<string> GetPassword(CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Password,
                description: StringUtil.Loc("Password"),
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator,
                token: token);
        }

        public async Task<string> GetPool(CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Pool,
                description: StringUtil.Loc("AgentMachinePoolNameLabel"),
                defaultValue: "default",
                validator: Validators.NonEmptyValidator,
                token: token);
        }

        public async Task<string> GetToken(CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Token,
                description: StringUtil.Loc("PersonalAccessToken"),
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator,
                token: token);
        }

        public async Task<string> GetUrl(CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Url,
                description: StringUtil.Loc("ServerUrl"),
                defaultValue: string.Empty,
                validator: Validators.ServerUrlValidator,
                token: token);
        }

        public async Task<string> GetUserName(CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.UserName,
                description: StringUtil.Loc("UserName"),
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator,
                token: token);
        }

        public async Task<string> GetWindowsLogonAccount(string defaultValue, CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.WindowsLogonAccount,
                description: StringUtil.Loc("WindowsLogonAccountNameDescription"),
                defaultValue: defaultValue,
                validator: Validators.NTAccountValidator,
                token: token);
        }

        public async Task<string> GetWindowsLogonPassword(string accountName, CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.WindowsLogonPassword,
                description: StringUtil.Loc("WindowsLogonPasswordDescription", accountName),
                defaultValue: string.Empty,
                validator: Validators.NonEmptyValidator,
                token: token);
        }

        public async Task<string> GetWork(CancellationToken token)
        {
            return await GetArgOrPrompt(
                name: Constants.Agent.CommandLine.Args.Work,
                description: StringUtil.Loc("WorkFolderDescription"),
                defaultValue: Constants.Path.WorkDirectory,
                validator: Validators.NonEmptyValidator,
                token: token);
        }

        public string GetNotificationPipeName()
        {
            return GetArg(Constants.Agent.CommandLine.Args.NotificationPipeName);
        }

        //
        // Private helpers.
        //
        private string GetArg(string name)
        {
            string result;
            if (!_parser.Args.TryGetValue(name, out result))
            {
                result = null;
            }

            return result;
        }

        private async Task<string> GetArgOrPrompt(
            string name,
            string description,
            string defaultValue,
            Func<string, bool> validator,
            CancellationToken token)
        {
            // Check for the arg in the command line parser.
            ArgUtil.NotNull(validator, nameof(validator));
            string result = GetArg(name);

            // Return the arg if it is not empty and is valid.
            _trace.Info($"Arg '{name}': '{result}'");
            if (!string.IsNullOrEmpty(result))
            {
                if (validator(result))
                {
                    return result;
                }

                _trace.Info("Arg is invalid.");
            }

            // Otherwise prompt for the arg.
            return await _promptManager.ReadValue(
                argName: name,
                description: description,
                secret: Constants.Agent.CommandLine.Args.Secrets.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)),
                defaultValue: defaultValue,
                validator: validator,
                unattended: Unattended,
                token: token);
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
            _trace.Info($"Flag '{name}': '{result}'");
            return result;
        }

        private async Task<bool> TestFlagOrPrompt(
            string name,
            string description,
            bool defaultValue,
            CancellationToken token)
        {
            bool result = TestFlag(name);
            if (!result)
            {
                result = await _promptManager.ReadBool(
                    argName: name,
                    description: description,
                    defaultValue: defaultValue,
                    unattended: Unattended,
                    token: token);
            }

            return result;
        }
    }
}
