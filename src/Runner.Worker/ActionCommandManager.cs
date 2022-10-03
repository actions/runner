﻿using GitHub.DistributedTask.Pipelines.ContextData;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Worker.Container;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(ActionCommandManager))]
    public interface IActionCommandManager : IRunnerService
    {
        void EnablePluginInternalCommand();
        void DisablePluginInternalCommand();
        bool TryProcessCommand(IExecutionContext context, string input, ContainerInfo container);
    }

    public sealed class ActionCommandManager : RunnerService, IActionCommandManager
    {
        private const string _stopCommand = "stop-commands";
        private readonly Dictionary<string, IActionCommandExtension> _commandExtensions = new Dictionary<string, IActionCommandExtension>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _registeredCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _commandSerializeLock = new object();
        private bool _stopProcessCommand = false;
        private string _stopToken = null;

        public override void Initialize(IHostContext hostContext)
        {
            base.Initialize(hostContext);

            _registeredCommands.Add(_stopCommand);

            // Register all command extensions
            var extensionManager = hostContext.GetService<IExtensionManager>();
            foreach (var commandExt in extensionManager.GetExtensions<IActionCommandExtension>() ?? new List<IActionCommandExtension>())
            {
                Trace.Info($"Register action command extension for command {commandExt.Command}");
                _commandExtensions[commandExt.Command] = commandExt;
                if (commandExt.Command != "internal-set-repo-path")
                {
                    _registeredCommands.Add(commandExt.Command);
                }
            }
        }

        public void EnablePluginInternalCommand()
        {
            Trace.Info($"Enable plugin internal command extension.");
            _registeredCommands.Add("internal-set-repo-path");
        }

        public void DisablePluginInternalCommand()
        {
            Trace.Info($"Disable plugin internal command extension.");
            _registeredCommands.Remove("internal-set-repo-path");
        }

        public bool TryProcessCommand(IExecutionContext context, string input, ContainerInfo container)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            // TryParse input to Command
            ActionCommand actionCommand;
            if (!ActionCommand.TryParseV2(input, _registeredCommands, out actionCommand) &&
                !ActionCommand.TryParse(input, _registeredCommands, out actionCommand))
            {
                return false;
            }

            if (!ActionCommandManager.EnhancedAnnotationsEnabled(context) && actionCommand.Command == "notice")
            {
                context.Debug($"Enhanced Annotations not enabled on the server: 'notice' command will not be processed.");
                return false;
            }

            // Serialize order
            lock (_commandSerializeLock)
            {
                // Currently stopped
                if (_stopProcessCommand)
                {
                    // Resume token
                    if (!string.IsNullOrEmpty(_stopToken) &&
                             string.Equals(actionCommand.Command, _stopToken, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Output(input);
                        context.Debug("Resume processing commands");
                        _registeredCommands.Remove(_stopToken);
                        _stopProcessCommand = false;
                        _stopToken = null;
                        return true;
                    }
                    else
                    {
                        context.Debug($"Process commands has been stopped and waiting for '##[{_stopToken}]' to resume.");
                        return false;
                    }
                }
                // Currently processing
                else
                {
                    // Stop command
                    if (string.Equals(actionCommand.Command, _stopCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        ValidateStopToken(context, actionCommand.Data);

                        _stopToken = actionCommand.Data;
                        _stopProcessCommand = true;
                        _registeredCommands.Add(_stopToken);
                        if (_stopToken.Length > 6)
                        {
                            HostContext.SecretMasker.AddValue(_stopToken);
                        }

                        context.Output(input);
                        context.Debug("Paused processing commands until the token you called ::stopCommands:: with is received");
                        return true;
                    }
                    // Found command
                    else if (_commandExtensions.TryGetValue(actionCommand.Command, out IActionCommandExtension extension))
                    {
                        if (context.EchoOnActionCommand && !extension.OmitEcho)
                        {
                            context.Output(input);
                        }

                        try
                        {
                            extension.ProcessCommand(context, input, actionCommand, container);
                        }
                        catch (Exception ex)
                        {
                            var commandInformation = extension.OmitEcho ? extension.Command : input;
                            context.Error($"Unable to process command '{commandInformation}' successfully.");
                            context.Error(ex);
                            context.CommandResult = TaskResult.Failed;
                        }
                    }
                    // Command not found
                    else
                    {
                        context.Warning($"Can't find command extension for ##[{actionCommand.Command}.command].");
                    }
                }
            }

            return true;
        }

        private void ValidateStopToken(IExecutionContext context, string stopToken)
        {
#if OS_WINDOWS
            var envContext = context.ExpressionValues["env"] as DictionaryContextData;
#else
            var envContext = context.ExpressionValues["env"] as CaseSensitiveDictionaryContextData;
#endif
            var allowUnsecureStopCommandTokens = false;
            allowUnsecureStopCommandTokens = StringUtil.ConvertToBoolean(Environment.GetEnvironmentVariable(Constants.Variables.Actions.AllowUnsupportedStopCommandTokens));
            if (!allowUnsecureStopCommandTokens && envContext.ContainsKey(Constants.Variables.Actions.AllowUnsupportedStopCommandTokens))
            {
                allowUnsecureStopCommandTokens = StringUtil.ConvertToBoolean(envContext[Constants.Variables.Actions.AllowUnsupportedStopCommandTokens].ToString());
            }

            bool isTokenInvalid = _registeredCommands.Contains(stopToken)
                || string.IsNullOrEmpty(stopToken)
                || string.Equals(stopToken, "pause-logging", StringComparison.OrdinalIgnoreCase);

            if (isTokenInvalid)
            {
                var telemetry = new JobTelemetry
                {
                    Message = $"Invoked ::stopCommand:: with token: [{stopToken}]",
                    Type = JobTelemetryType.ActionCommand
                };
                context.Global.JobTelemetry.Add(telemetry);
            }

            if (isTokenInvalid && !allowUnsecureStopCommandTokens)
            {
                throw new Exception(Constants.Runner.UnsupportedStopCommandTokenDisabled);
            }
        }

        internal static bool EnhancedAnnotationsEnabled(IExecutionContext context)
        {
            return context.Global.Variables.GetBoolean("DistributedTask.EnhancedAnnotations") ?? false;
        }
    }

    public interface IActionCommandExtension : IExtension
    {
        string Command { get; }
        bool OmitEcho { get; }

        void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container);
    }

    public sealed class InternalPluginSetRepoPathCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "internal-set-repo-path";
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            if (!command.Properties.TryGetValue(SetRepoPathCommandProperties.repoFullName, out string repoFullName) || string.IsNullOrEmpty(repoFullName))
            {
                throw new Exception("Required field 'repoFullName' is missing in ##[internal-set-repo-path] command.");
            }

            if (!command.Properties.TryGetValue(SetRepoPathCommandProperties.workspaceRepo, out string workspaceRepo) || string.IsNullOrEmpty(workspaceRepo))
            {
                throw new Exception("Required field 'workspaceRepo' is missing in ##[internal-set-repo-path] command.");
            }

            var directoryManager = HostContext.GetService<IPipelineDirectoryManager>();
            var trackingConfig = directoryManager.UpdateRepositoryDirectory(context, repoFullName, command.Data, StringUtil.ConvertToBoolean(workspaceRepo));
        }

        private static class SetRepoPathCommandProperties
        {
            public const String repoFullName = "repoFullName";
            public const String workspaceRepo = "workspaceRepo";
        }
    }

    public sealed class SetEnvCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "set-env";
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            var allowUnsecureCommands = false;
            bool.TryParse(Environment.GetEnvironmentVariable(Constants.Variables.Actions.AllowUnsupportedCommands), out allowUnsecureCommands);

            // Apply environment from env context, env context contains job level env and action's env block
#if OS_WINDOWS
            var envContext = context.ExpressionValues["env"] as DictionaryContextData;
#else
            var envContext = context.ExpressionValues["env"] as CaseSensitiveDictionaryContextData;
#endif
            if (!allowUnsecureCommands && envContext.ContainsKey(Constants.Variables.Actions.AllowUnsupportedCommands))
            {
                bool.TryParse(envContext[Constants.Variables.Actions.AllowUnsupportedCommands].ToString(), out allowUnsecureCommands);
            }

            if (!allowUnsecureCommands)
            {
                throw new Exception(String.Format(Constants.Runner.UnsupportedCommandMessageDisabled, this.Command));
            }

            if (!command.Properties.TryGetValue(SetEnvCommandProperties.Name, out string envName) || string.IsNullOrEmpty(envName))
            {
                throw new Exception("Required field 'name' is missing in ##[set-env] command.");
            }


            foreach (var blocked in _setEnvBlockList)
            {
                if (string.Equals(blocked, envName, StringComparison.OrdinalIgnoreCase))
                {
                    // Log Telemetry and let user know they shouldn't do this
                    var issue = new Issue()
                    {
                        Type = IssueType.Error,
                        Message = $"Can't update {blocked} environment variable using ::set-env:: command."
                    };
                    issue.Data[Constants.Runner.InternalTelemetryIssueDataKey] = $"{Constants.Runner.UnsupportedCommand}_{envName}";
                    context.AddIssue(issue);

                    return;
                }
            }

            context.Global.EnvironmentVariables[envName] = command.Data;
            context.SetEnvContext(envName, command.Data);
            context.Debug($"{envName}='{command.Data}'");
        }

        private static class SetEnvCommandProperties
        {
            public const String Name = "name";
        }

        private string[] _setEnvBlockList =
        {
            "NODE_OPTIONS"
        };
    }

    public sealed class SetOutputCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "set-output";
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            if (context.Global.Variables.GetBoolean("DistributedTask.DeprecateStepOutputCommands") ?? false)
            {
                var issue = new Issue()
                {
                    Type = IssueType.Warning,
                    Message = String.Format(Constants.Runner.UnsupportedCommandMessage, this.Command)
                };
                issue.Data[Constants.Runner.InternalTelemetryIssueDataKey] = Constants.Runner.UnsupportedCommand;
                context.AddIssue(issue);
            }

            if (!command.Properties.TryGetValue(SetOutputCommandProperties.Name, out string outputName) || string.IsNullOrEmpty(outputName))
            {
                throw new Exception("Required field 'name' is missing in ##[set-output] command.");
            }

            context.SetOutput(outputName, command.Data, out var reference);
            context.Debug($"{reference}='{command.Data}'");
        }

        private static class SetOutputCommandProperties
        {
            public const String Name = "name";
        }
    }

    public sealed class SaveStateCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "save-state";
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            if (context.Global.Variables.GetBoolean("DistributedTask.DeprecateStepOutputCommands") ?? false)
            {
                var issue = new Issue()
                {
                    Type = IssueType.Warning,
                    Message = String.Format(Constants.Runner.UnsupportedCommandMessage, this.Command)
                };
                issue.Data[Constants.Runner.InternalTelemetryIssueDataKey] = Constants.Runner.UnsupportedCommand;
                context.AddIssue(issue);
            }

            if (!command.Properties.TryGetValue(SaveStateCommandProperties.Name, out string stateName) || string.IsNullOrEmpty(stateName))
            {
                throw new Exception("Required field 'name' is missing in ##[save-state] command.");
            }
            // Embedded steps (composite) keep track of the state at the root level
            if (context.IsEmbedded)
            {
                var id = context.EmbeddedId;
                if (!context.Root.EmbeddedIntraActionState.ContainsKey(id))
                {
                    context.Root.EmbeddedIntraActionState[id] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                context.Root.EmbeddedIntraActionState[id][stateName] = command.Data;
            }
            // Otherwise modify the ExecutionContext
            else
            {
                context.IntraActionState[stateName] = command.Data;
            }
            context.Debug($"Save intra-action state {stateName} = {command.Data}");
        }

        private static class SaveStateCommandProperties
        {
            public const String Name = "name";
        }
    }

    public sealed class AddMaskCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "add-mask";
        public bool OmitEcho => true;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            if (string.IsNullOrWhiteSpace(command.Data))
            {
                context.Warning("Can't add secret mask for empty string in ##[add-mask] command.");
            }
            else
            {
                if (context.EchoOnActionCommand)
                {
                    context.Output($"::{Command}::***");
                }

                HostContext.SecretMasker.AddValue(command.Data);
                Trace.Info($"Add new secret mask with length of {command.Data.Length}");

                // Also add each individual line. Typically individual lines are processed from STDOUT of child processes.
                var split = command.Data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var item in split)
                {
                    HostContext.SecretMasker.AddValue(item);
                }
            }
        }
    }

    public sealed class AddPathCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "add-path";
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            var allowUnsecureCommands = false;
            bool.TryParse(Environment.GetEnvironmentVariable(Constants.Variables.Actions.AllowUnsupportedCommands), out allowUnsecureCommands);

            // Apply environment from env context, env context contains job level env and action's env block
#if OS_WINDOWS
            var envContext = context.ExpressionValues["env"] as DictionaryContextData;
#else
            var envContext = context.ExpressionValues["env"] as CaseSensitiveDictionaryContextData;
#endif
            if (!allowUnsecureCommands && envContext.ContainsKey(Constants.Variables.Actions.AllowUnsupportedCommands))
            {
                bool.TryParse(envContext[Constants.Variables.Actions.AllowUnsupportedCommands].ToString(), out allowUnsecureCommands);
            }

            if (!allowUnsecureCommands)
            {
                throw new Exception(String.Format(Constants.Runner.UnsupportedCommandMessageDisabled, this.Command));
            }

            ArgUtil.NotNullOrEmpty(command.Data, "path");
            context.Global.PrependPath.RemoveAll(x => string.Equals(x, command.Data, StringComparison.CurrentCulture));
            context.Global.PrependPath.Add(command.Data);
        }
    }

    public sealed class AddMatcherCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "add-matcher";
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            var file = command.Data;

            // File is required
            if (string.IsNullOrEmpty(file))
            {
                context.Warning("File path must be specified.");
                return;
            }

            // Translate file path back from container path
            if (container != null)
            {
                file = container.TranslateToHostPath(file);
            }

            // Root the path
            if (!Path.IsPathRooted(file))
            {
                var githubContext = context.ExpressionValues["github"] as GitHubContext;
                ArgUtil.NotNull(githubContext, nameof(githubContext));
                var workspace = githubContext["workspace"].ToString();
                ArgUtil.NotNullOrEmpty(workspace, "workspace");

                file = Path.Combine(workspace, file);
            }

            // Load the config
            var config = IOUtil.LoadObject<IssueMatchersConfig>(file);

            // Add
            if (config?.Matchers?.Count > 0)
            {
                config.Validate();
                context.AddMatchers(config);
            }
        }
    }

    public sealed class RemoveMatcherCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "remove-matcher";
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            command.Properties.TryGetValue(RemoveMatcherCommandProperties.Owner, out string owner);
            var file = command.Data;

            // Owner and file are mutually exclusive
            if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(file))
            {
                context.Warning("Either specify an owner name or a file path in ##[remove-matcher] command. Both values cannot be set.");
                return;
            }

            // Owner or file is required
            if (string.IsNullOrEmpty(owner) && string.IsNullOrEmpty(file))
            {
                context.Warning("Either an owner name or a file path must be specified in ##[remove-matcher] command.");
                return;
            }

            // Remove by owner
            if (!string.IsNullOrEmpty(owner))
            {
                context.RemoveMatchers(new[] { owner });
            }
            // Remove by file
            else
            {
                // Translate file path back from container path
                if (container != null)
                {
                    file = container.TranslateToHostPath(file);
                }

                // Root the path
                if (!Path.IsPathRooted(file))
                {
                    var githubContext = context.ExpressionValues["github"] as GitHubContext;
                    ArgUtil.NotNull(githubContext, nameof(githubContext));
                    var workspace = githubContext["workspace"].ToString();
                    ArgUtil.NotNullOrEmpty(workspace, "workspace");

                    file = Path.Combine(workspace, file);
                }

                // Load the config
                var config = IOUtil.LoadObject<IssueMatchersConfig>(file);

                if (config?.Matchers?.Count > 0)
                {
                    // Remove
                    context.RemoveMatchers(config.Matchers.Select(x => x.Owner));
                }
            }
        }

        private static class RemoveMatcherCommandProperties
        {
            public const string Owner = "owner";
        }
    }

    public sealed class DebugCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "debug";
        public bool OmitEcho => true;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string inputLine, ActionCommand command, ContainerInfo container)
        {
            context.Debug(command.Data);
        }
    }

    public sealed class WarningCommandExtension : IssueCommandExtension
    {
        public override IssueType Type => IssueType.Warning;

        public override string Command => "warning";
    }

    public sealed class ErrorCommandExtension : IssueCommandExtension
    {
        public override IssueType Type => IssueType.Error;

        public override string Command => "error";
    }

    public sealed class NoticeCommandExtension : IssueCommandExtension
    {
        public override IssueType Type => IssueType.Notice;

        public override string Command => "notice";
    }

    public abstract class IssueCommandExtension : RunnerService, IActionCommandExtension
    {
        public abstract IssueType Type { get; }
        public abstract string Command { get; }
        public bool OmitEcho => true;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string inputLine, ActionCommand command, ContainerInfo container)
        {
            ValidateLinesAndColumns(command, context);

            command.Properties.TryGetValue(IssueCommandProperties.File, out string file);
            command.Properties.TryGetValue(IssueCommandProperties.Line, out string line);
            command.Properties.TryGetValue(IssueCommandProperties.Column, out string column);

            if (!ActionCommandManager.EnhancedAnnotationsEnabled(context))
            {
                context.Debug("Enhanced Annotations not enabled on the server. The 'title', 'end_line', and 'end_column' fields are unsupported.");
            }

            Issue issue = new Issue()
            {
                Category = "General",
                Type = this.Type,
                Message = command.Data
            };

            if (!string.IsNullOrEmpty(file))
            {
                issue.Category = "Code";

                if (container != null)
                {
                    // Translate file path back from container path
                    file = container.TranslateToHostPath(file);
                    command.Properties[IssueCommandProperties.File] = file;
                }

                // Get the values that represent the server path given a local path
                string repoName = context.GetGitHubContext("repository");
                var repoPath = context.GetGitHubContext("workspace");

                string relativeSourcePath = IOUtil.MakeRelative(file, repoPath);
                if (!string.Equals(relativeSourcePath, file, IOUtil.FilePathStringComparison))
                {
                    // add repo info
                    if (!string.IsNullOrEmpty(repoName))
                    {
                        command.Properties["repo"] = repoName;
                    }

                    if (!string.IsNullOrEmpty(relativeSourcePath))
                    {
                        // replace sourcePath with the new relative path
                        // prefer `/` on all platforms
                        command.Properties[IssueCommandProperties.File] = relativeSourcePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    }
                }
            }

            foreach (var property in command.Properties)
            {
                if (!string.Equals(property.Key, Constants.Runner.InternalTelemetryIssueDataKey, StringComparison.OrdinalIgnoreCase))
                {
                    issue.Data[property.Key] = property.Value;
                }
            }

            context.AddIssue(issue);
        }

        public static void ValidateLinesAndColumns(ActionCommand command, IExecutionContext context)
        {
            command.Properties.TryGetValue(IssueCommandProperties.Line, out string line);
            command.Properties.TryGetValue(IssueCommandProperties.EndLine, out string endLine);
            command.Properties.TryGetValue(IssueCommandProperties.Column, out string column);
            command.Properties.TryGetValue(IssueCommandProperties.EndColumn, out string endColumn);

            var hasStartLine = int.TryParse(line, out int lineNumber);
            var hasEndLine = int.TryParse(endLine, out int endLineNumber);
            var hasStartColumn = int.TryParse(column, out int columnNumber);
            var hasEndColumn = int.TryParse(endColumn, out int endColumnNumber);
            var hasColumn = hasStartColumn || hasEndColumn;

            if (hasEndLine && !hasStartLine)
            {
                context.Debug($"Invalid {command.Command} command value. '{IssueCommandProperties.EndLine}' can only be set if '{IssueCommandProperties.Line}' is provided");
                command.Properties[IssueCommandProperties.Line] = endLine;
                hasStartLine = true;
                line = endLine;
            }

            if (hasEndColumn && !hasStartColumn)
            {
                context.Debug($"Invalid {command.Command} command value. '{IssueCommandProperties.EndColumn}' can only be set if '{IssueCommandProperties.Column}' is provided");
                command.Properties[IssueCommandProperties.Column] = endColumn;
                hasStartColumn = true;
                column = endColumn;
            }

            if (!hasStartLine && hasColumn)
            {
                context.Debug($"Invalid {command.Command} command value. '{IssueCommandProperties.Column}' and '{IssueCommandProperties.EndColumn}' can only be set if '{IssueCommandProperties.Line}' value is provided.");
                command.Properties.Remove(IssueCommandProperties.Column);
                command.Properties.Remove(IssueCommandProperties.EndColumn);
            }

            if (hasEndLine && line != endLine && hasColumn)
            {
                context.Debug($"Invalid {command.Command} command value. '{IssueCommandProperties.Column}' and '{IssueCommandProperties.EndColumn}' cannot be set if '{IssueCommandProperties.Line}' and '{IssueCommandProperties.EndLine}' are different values.");
                command.Properties.Remove(IssueCommandProperties.Column);
                command.Properties.Remove(IssueCommandProperties.EndColumn);
            }

            if (hasStartLine && hasEndLine && endLineNumber < lineNumber)
            {
                context.Debug($"Invalid {command.Command} command value. '{IssueCommandProperties.EndLine}' cannot be less than '{IssueCommandProperties.Line}'.");
                command.Properties.Remove(IssueCommandProperties.Line);
                command.Properties.Remove(IssueCommandProperties.EndLine);
            }

            if (hasStartColumn && hasEndColumn && endColumnNumber < columnNumber)
            {
                context.Debug($"Invalid {command.Command} command value. '{IssueCommandProperties.EndColumn}' cannot be less than '{IssueCommandProperties.Column}'.");
                command.Properties.Remove(IssueCommandProperties.Column);
                command.Properties.Remove(IssueCommandProperties.EndColumn);
            }
        }

        private static class IssueCommandProperties
        {
            public const String File = "file";
            public const String Line = "line";
            public const String EndLine = "endLine";
            public const String Column = "col";
            public const String EndColumn = "endColumn";
            public const String Title = "title";
        }
    }

    public sealed class GroupCommandExtension : GroupingCommandExtension
    {
        public override string Command => "group";
    }

    public sealed class EndGroupCommandExtension : GroupingCommandExtension
    {
        public override string Command => "endgroup";
    }

    public abstract class GroupingCommandExtension : RunnerService, IActionCommandExtension
    {
        public abstract string Command { get; }
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            var data = this is GroupCommandExtension ? command.Data : string.Empty;
            context.Output($"##[{Command}]{data}");
        }
    }

    public sealed class EchoCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "echo";
        public bool OmitEcho => false;

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, ContainerInfo container)
        {
            ArgUtil.NotNullOrEmpty(command.Data, "value");

            switch (command.Data.Trim().ToUpperInvariant())
            {
                case "ON":
                    context.EchoOnActionCommand = true;
                    context.Debug("Setting echo command value to 'on'");
                    break;
                case "OFF":
                    context.EchoOnActionCommand = false;
                    context.Debug("Setting echo command value to 'off'");
                    break;
                default:
                    throw new Exception($"Invalid echo command value. Possible values can be: 'on', 'off'. Current value is: '{command.Data}'.");
            }
        }
    }
}
