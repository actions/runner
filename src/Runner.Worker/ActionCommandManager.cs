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
                        bool commandHasBeenOutput = false;

                        try
                        {
                            if (context.EchoOnActionCommand)
                            {
                                context.Output(input);
                                context.Debug($"Processing command '{actionCommand.Command}'");
                                commandHasBeenOutput = true;
                            }

                            extension.ProcessCommand(context, input, actionCommand);

                            if (context.EchoOnActionCommand)
                            {
                                context.Debug($"Processed command '{actionCommand.Command}' successfully");
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!commandHasBeenOutput)
                            {
                                context.Output(input);
                            }

                            context.Error($"Unable to process command '{input}' successfully.");
                            context.Error(ex);
                            context.CommandResult = TaskResult.Failed;
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

        void ProcessCommand(IExecutionContext context, string line, ActionCommand command);
    }

    public sealed class InternalPluginSetRepoPathCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "internal-set-repo-path";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
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

    public sealed class SetWorkspaceCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "set-workspace";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
        {
            string path = command.Data;
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new Exception("Path not specified");
            }

            if (!Path.IsPathRooted(path))
            {
                throw new Exception("Expected a rooted path");
            }

            string runnerWorkspace = context.GetRunnerContext("workspace");
            ArgUtil.Directory(runnerWorkspace, nameof(runnerWorkspace));

            // Must be under runner workspace
            path = Path.GetFullPath(path); // remove relative pathing and normalize slashes
            if (!path.StartsWith(runnerWorkspace + Path.DirectorySeparatorChar, IOUtil.FilePathStringComparison))
            {
                throw new Exception($"Expected path to be under {runnerWorkspace}");
            }

            Trace.Info($"Setting GitHub workspace to '{path}'");
            context.SetGitHubContext("workspace", path);
        }
    }

    public sealed class SetEnvCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "set-env";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
        {
            if (!command.Properties.TryGetValue(SetEnvCommandProperties.Name, out string envName) || string.IsNullOrEmpty(envName))
            {
                throw new Exception("Required field 'name' is missing in ##[set-env] command.");
            }

            context.EnvironmentVariables[envName] = command.Data;
            context.SetEnvContext(envName, command.Data);
            context.Debug($"{envName}='{command.Data}'");
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

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
        {
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

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
        {
            if (!command.Properties.TryGetValue(SaveStateCommandProperties.Name, out string stateName) || string.IsNullOrEmpty(stateName))
            {
                throw new Exception("Required field 'name' is missing in ##[save-state] command.");
            }

            context.IntraActionState[stateName] = command.Data;
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

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Data))
            {
                context.Warning("Can't add secret mask for empty string in ##[add-mask] command.");
            }
            else
            {
                HostContext.SecretMasker.AddValue(command.Data);
                Trace.Info($"Add new secret mask with length of {command.Data.Length}");
            }
        }
    }

    public sealed class AddPathCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "add-path";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
        {
            ArgUtil.NotNullOrEmpty(command.Data, "path");
            context.PrependPath.RemoveAll(x => string.Equals(x, command.Data, StringComparison.CurrentCulture));
            context.PrependPath.Add(command.Data);
        }
    }

    public sealed class AddMatcherCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "add-matcher";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
        {
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

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
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

        public void ProcessCommand(IExecutionContext context, string inputLine, ActionCommand command)
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

    public abstract class IssueCommandExtension : RunnerService, IActionCommandExtension
    {
        public abstract IssueType Type { get; }
        public abstract string Command { get; }

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string inputLine, ActionCommand command)
        {
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
                        // prefer `/` on all platforms
                        command.Properties[IssueCommandProperties.File] = relativeSourcePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
        {
            var data = this is GroupCommandExtension ? command.Data : string.Empty;
            context.Output($"##[{Command}]{data}");
        }
    }

    public sealed class EchoCommandExtension : RunnerService, IActionCommandExtension
    {
        public string Command => "echo";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command)
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
                    break;
            }
        }
    }
}
