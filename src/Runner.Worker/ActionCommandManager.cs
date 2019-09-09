using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
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
        bool TryProcessCommand(IExecutionContext context, string input);
    }

    public sealed class ActionCommandManager : RunnerService, IActionCommandManager
    {
        private const string _stopCommand = "stop-commands";
        private readonly Dictionary<string, IActionCommandExtension> _commandExtensions = new Dictionary<string, IActionCommandExtension>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _registeredCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

        public bool TryProcessCommand(IExecutionContext context, string input)
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

            // process action command in serialize oreder.
            lock (_commandSerializeLock)
            {
                if (_stopProcessCommand)
                {
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
                else
                {
                    if (string.Equals(actionCommand.Command, _stopCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Output(input);
                        context.Debug("Paused processing commands until '##[{actionCommand.Data}]' is received");
                        _stopToken = actionCommand.Data;
                        _stopProcessCommand = true;
                        _registeredCommands.Add(_stopToken);
                        return true;
                    }
                    else if (_commandExtensions.TryGetValue(actionCommand.Command, out IActionCommandExtension extension))
                    {
                        bool omitEcho;
                        try
                        {
                            extension.ProcessCommand(context, input, actionCommand, out omitEcho);
                        }
                        catch (Exception ex)
                        {
                            omitEcho = true;
                            context.Output(input);
                            context.Error($"Unable to process command '{input}' successfully.");
                            context.Error(ex);
                            context.CommandResult = TaskResult.Failed;
                        }

                        if (!omitEcho)
                        {
                            context.Output(input);
                            context.Debug($"Processed command");
                        }

                    }
                    else
                    {
                        context.Warning($"Can't find command extension for ##[{actionCommand.Command}.command].");
                    }
                }
            }

            return true;
        }
    }

    public interface IActionCommandExtension : IExtension
    {
        string Command { get; }

        void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho);
    }

    public sealed class InternalPluginSetRepoPathCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "internal-set-repo-path";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
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

            omitEcho = true;
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

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            if (!command.Properties.TryGetValue(SetEnvCommandProperties.Name, out string envName) || string.IsNullOrEmpty(envName))
            {
                throw new Exception("Required field 'name' is missing in ##[set-env] command.");
            }

            context.EnvironmentVariables[envName] = command.Data;
            context.Output(line);
            context.Debug($"{envName}='{command.Data}'");
            omitEcho = true;
        }

        private static class SetEnvCommandProperties
        {
            public const String Name = "name";
        }
    }

    public sealed class SetOutputCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "set-output";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            if (!command.Properties.TryGetValue(SetOutputCommandProperties.Name, out string outputName) || string.IsNullOrEmpty(outputName))
            {
                throw new Exception("Required field 'name' is missing in ##[set-output] command.");
            }

            if (command.Properties.TryGetValue(SetOutputCommandProperties.Name, out string isSecretString) && Boolean.TryParse(isSecretString, out Boolean isSecret) && isSecret)
            {
                // Mask secret
                if (string.IsNullOrWhiteSpace(command.Data))
                {
                    context.Warning("Can't add secret mask for empty string.");
                }
                else
                {
                    HostContext.SecretMasker.AddValue(command.Data);
                    Trace.Info($"Add new secret mask with length of {command.Data.Length}");
                }

                context.SetOutput(outputName, command.Data, out var reference);
            }
            else
            {
                context.SetOutput(outputName, command.Data, out var reference);
                context.Output(line);
                context.Debug($"{reference}='{command.Data}'");
            }

            omitEcho = true;
        }

        private static class SetOutputCommandProperties
        {
            public const String Name = "name";
            public const String IsSecret = "isSecret";
        }
    }

    public sealed class SetSecretCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "set-secret";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            if (!command.Properties.TryGetValue(SetSecretCommandProperties.Name, out string secretName) || string.IsNullOrEmpty(secretName))
            {
                throw new Exception("Required field 'name' is missing in ##[set-secret] command.");
            }

            // Mask secret
            if (string.IsNullOrWhiteSpace(command.Data))
            {
                context.Warning("Can't add secret mask for empty string.");
            }
            else
            {
                HostContext.SecretMasker.AddValue(command.Data);
                Trace.Info($"Add new secret mask with length of {command.Data.Length}");
            }

            // Set secret as output
            context.SetSecret(secretName, command.Data);
            omitEcho = true;
        }

        private static class SetSecretCommandProperties
        {
            public const String Name = "name";
        }
    }

    public sealed class AddMaskCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "add-mask";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            if (string.IsNullOrWhiteSpace(command.Data))
            {
                context.Warning("Can't add secret mask for empty string.");
            }
            else
            {
                HostContext.SecretMasker.AddValue(command.Data);
                Trace.Info($"Add new secret mask with length of {command.Data.Length}");
            }

            omitEcho = true;
        }
    }

    public sealed class AddPathCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "add-path";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            ArgUtil.NotNullOrEmpty(command.Data, "path");
            context.PrependPath.RemoveAll(x => string.Equals(x, command.Data, StringComparison.CurrentCulture));
            context.PrependPath.Add(command.Data);
            omitEcho = false;
        }
    }

    public sealed class AddMatcherCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "add-matcher";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            omitEcho = false;
            var file = command.Data;

            // File is required
            if (string.IsNullOrEmpty(file))
            {
                context.Warning("File path must be specified.");
                return;
            }

            // Translate file path back from container path
            if (context.Container != null)
            {
                file = context.Container.TranslateToHostPath(file);
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

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            omitEcho = false;
            command.Properties.TryGetValue(RemoveMatcherCommandProperties.Owner, out string owner);
            var file = command.Data;

            // Owner and file are mutually exclusive
            if (!string.IsNullOrEmpty(owner) && !string.IsNullOrEmpty(file))
            {
                context.Warning("Either specify a matcher owner name or a file path. Both values cannot be set.");
                return;
            }

            // Owner or file is required
            if (string.IsNullOrEmpty(owner) && string.IsNullOrEmpty(file))
            {
                context.Warning("Either a matcher owner name or a file path must be specified.");
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
                if (context.Container != null)
                {
                    file = context.Container.TranslateToHostPath(file);
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

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string inputLine, ActionCommand command, out bool omitEcho)
        {
            omitEcho = true;
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

    public abstract class IssueCommandExtension : RunnerService, IActionCommandExtension
    {
        public abstract IssueType Type { get; }
        public abstract string Command { get; }

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string inputLine, ActionCommand command, out bool omitEcho)
        {
            omitEcho = true;
            command.Properties.TryGetValue(IssueCommandProperties.File, out string file);
            command.Properties.TryGetValue(IssueCommandProperties.Line, out string line);
            command.Properties.TryGetValue(IssueCommandProperties.Column, out string column);

            Issue issue = new Issue()
            {
                Category = "General",
                Type = this.Type,
                Message = command.Data
            };

            if (!string.IsNullOrEmpty(file))
            {
                issue.Category = "Code";

                if (context.Container != null)
                {
                    // Translate file path back from container path
                    file = context.Container.TranslateToHostPath(file);
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
                        command.Properties[IssueCommandProperties.File] = relativeSourcePath;
                    }
                }
            }

            foreach (var property in command.Properties)
            {
                issue.Data[property.Key] = property.Value;
            }

            context.AddIssue(issue);
        }

        private static class IssueCommandProperties
        {
            public const String File = "file";
            public const String Line = "line";
            public const String Column = "col";
        }
    }
}
