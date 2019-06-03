using GitHub.DistributedTask.Pipelines;
using GitHub.DistributedTask.WebApi;
using Runner.Common.Util;
using Runner.Common.Worker.Build;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Runner.Common.Worker
{
    [ServiceLocator(Default = typeof(ActionCommandManager))]
    public interface IActionCommandManager : IAgentService
    {
        void EnablePluginInternalCommand();
        void DisablePluginInternalCommand();
        bool TryProcessCommand(IExecutionContext context, string input);
    }

    public sealed class ActionCommandManager : AgentService, IActionCommandManager
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
                if (commandExt.Command != "internal-set-self-path")
                {
                    _registeredCommands.Add(commandExt.Command);
                }
            }
        }

        public void EnablePluginInternalCommand()
        {
            Trace.Info($"Enable plugin internal command extension.");
            _registeredCommands.Add("internal-set-self-path");
        }

        public void DisablePluginInternalCommand()
        {
            Trace.Info($"Disable plugin internal command extension.");
            _registeredCommands.Remove("internal-set-self-path");
        }

        public bool TryProcessCommand(IExecutionContext context, string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            // TryParse input to Command
            ActionCommand actionCommand;
            if (!ActionCommand.TryParse(input, _registeredCommands, out actionCommand))
            {
                return false;
            }

            // process action command in serialize oreder.
            lock (_commandSerializeLock)
            {
                if (_stopProcessCommand)
                {
                    context.Debug($"Process commands has been stopped and waiting for '##[{_stopToken}]' to resume.");
                    return false;
                }
                else
                {
                    if (string.Equals(actionCommand.Command, _stopCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Output(input);
                        context.Output($"{WellKnownTags.Debug}Paused processing commands until '##[{actionCommand.Data}]' is received");
                        _stopToken = actionCommand.Data;
                        _stopProcessCommand = true;
                        _registeredCommands.Add(_stopToken);
                        return true;
                    }
                    else if (!string.IsNullOrEmpty(_stopToken) &&
                             string.Equals(actionCommand.Command, _stopToken, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Output(input);
                        context.Output($"{WellKnownTags.Debug}Resume processing commands");
                        _registeredCommands.Remove(_stopToken);
                        _stopProcessCommand = false;
                        _stopToken = null;
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
                            context.Error(StringUtil.Loc("CommandProcessFailed", input));
                            context.Error(ex);
                            context.CommandResult = TaskResult.Failed;
                        }

                        if (!omitEcho)
                        {
                            context.Output(input);
                            context.Output($"{WellKnownTags.Debug}Processed command");
                        }

                    }
                    else
                    {
                        context.Warning(StringUtil.Loc("CommandNotFound", actionCommand.Command));
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

    public sealed class InternalPluginSetRepoPathCommandExtension : AgentService, IActionCommandExtension
    {
        public string Command => "internal-set-self-path";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            var selfRepo = context.Repositories.Single(x => string.Equals(x.Alias, PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase));
            selfRepo.Properties.Set(RepositoryPropertyNames.Path, command.Data);
            context.SetGitHubContext("workspace", command.Data);

            var directoryManager = HostContext.GetService<IBuildDirectoryManager>();
            var trackingConfig = directoryManager.UpdateDirectory(context, selfRepo);

            omitEcho = true;
        }
    }

    public sealed class SetEnvCommandExtension : AgentService, IActionCommandExtension
    {
        public string Command => "set-env";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            if (!command.Properties.TryGetValue(SetEnvCommandProperties.Name, out string envName) || string.IsNullOrEmpty(envName))
            {
                throw new Exception(StringUtil.Loc("MissingEnvName"));
            }

            context.EnvironmentVariables[envName] = command.Data;
            context.Output(line);
            context.Output($"{WellKnownTags.Debug}{envName}='{command.Data}'");
            omitEcho = true;
        }

        private static class SetEnvCommandProperties
        {
            public const String Name = "name";
        }
    }

    public sealed class SetOutputCommandExtension : AgentService, IActionCommandExtension
    {
        public string Command => "set-output";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            if (!command.Properties.TryGetValue(SetOutputCommandProperties.Name, out string outputName) || string.IsNullOrEmpty(outputName))
            {
                throw new Exception(StringUtil.Loc("MissingOutputName"));
            }

            context.SetOutput(outputName, command.Data, out var reference);
            context.Output(line);
            context.Output($"{WellKnownTags.Debug}{reference}='{command.Data}'");
            omitEcho = true;
        }

        private static class SetOutputCommandProperties
        {
            public const String Name = "name";
        }
    }

    public sealed class SetSecretCommandExtension : AgentService, IActionCommandExtension
    {
        public string Command => "set-secret";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string line, ActionCommand command, out bool omitEcho)
        {
            if (!command.Properties.TryGetValue(SetSecretCommandProperties.Name, out string secretName) || string.IsNullOrEmpty(secretName))
            {
                throw new Exception(StringUtil.Loc("MissingSecretName"));
            }

            throw new NotSupportedException("Not supported yet");
        }

        private static class SetSecretCommandProperties
        {
            public const String Name = "name";
        }
    }

    public sealed class AddPathCommandExtension : AgentService, IActionCommandExtension
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

    public abstract class IssueCommandExtension : AgentService, IActionCommandExtension
    {
        public abstract IssueType Type { get; }
        public abstract string Command { get; }

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, string inputLine, ActionCommand command, out bool omitEcho)
        {
            omitEcho = false;
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
                var selfRepo = context.Repositories.Single(x => x.Alias == PipelineConstants.SelfAlias);
                string repoName = selfRepo.Properties.Get<string>(RepositoryPropertyNames.Name);
                var repoPath = selfRepo.Properties.Get<string>(RepositoryPropertyNames.Path);


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
