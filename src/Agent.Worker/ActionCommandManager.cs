using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    [ServiceLocator(Default = typeof(ActionCommandManager))]
    public interface IActionCommandManager : IAgentService
    {
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
                _registeredCommands.Add(commandExt.Command);
            }
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
                        context.Output($"Stop process further commands till '##[{actionCommand.Data}]' come in.");
                        _stopToken = actionCommand.Data;
                        _stopProcessCommand = true;
                        _registeredCommands.Add(_stopToken);
                        return true;
                    }
                    else if (!string.IsNullOrEmpty(_stopToken) &&
                             string.Equals(actionCommand.Command, _stopToken, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Output($"Resume process further commands.");
                        _registeredCommands.Remove(_stopToken);
                        _stopProcessCommand = false;
                        _stopToken = null;
                        return true;
                    }
                    else if (_commandExtensions.TryGetValue(actionCommand.Command, out IActionCommandExtension extension))
                    {
                        try
                        {
                            extension.ProcessCommand(context, actionCommand);
                        }
                        catch (Exception ex)
                        {
                            context.Error(StringUtil.Loc("CommandProcessFailed", input));
                            context.Error(ex);
                            context.CommandResult = TaskResult.Failed;
                        }
                        finally
                        {
                            context.Debug($"Processed: {input}");
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

        void ProcessCommand(IExecutionContext context, ActionCommand command);
    }

    public sealed class SetEnvCommandExtension : AgentService, IActionCommandExtension
    {
        public string Command => "set-env";

        public Type ExtensionType => typeof(IActionCommandExtension);

        public void ProcessCommand(IExecutionContext context, ActionCommand command)
        {
            if (!command.Properties.TryGetValue(SetEnvCommandProperties.Name, out string envName) || string.IsNullOrEmpty(envName))
            {
                throw new Exception(StringUtil.Loc("MissingEnvName"));
            }

            context.SetVariable(envName, command.Data);
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

        public void ProcessCommand(IExecutionContext context, ActionCommand command)
        {
            if (!command.Properties.TryGetValue(SetOutputCommandProperties.Name, out string outputName) || string.IsNullOrEmpty(outputName))
            {
                throw new Exception(StringUtil.Loc("MissingOutputName"));
            }

            context.SetOutput(outputName, command.Data);
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

        public void ProcessCommand(IExecutionContext context, ActionCommand command)
        {
            if (!command.Properties.TryGetValue(SetSecretCommandProperties.Name, out string secretName) || string.IsNullOrEmpty(secretName))
            {
                throw new Exception(StringUtil.Loc("MissingSecretName"));
            }

            context.SetOutput(secretName, command.Data, isSecret: true);
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

        public void ProcessCommand(IExecutionContext context, ActionCommand command)
        {
            ArgUtil.NotNullOrEmpty(command.Data, "path");
            context.PrependPath.RemoveAll(x => string.Equals(x, command.Data, StringComparison.CurrentCulture));
            context.PrependPath.Add(command.Data);
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

        public void ProcessCommand(IExecutionContext context, ActionCommand command)
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

                var extensionManager = HostContext.GetService<IExtensionManager>();
                var hostType = context.Variables.System_HostType;
                IJobExtension extension =
                    (extensionManager.GetExtensions<IJobExtension>() ?? new List<IJobExtension>())
                    .Where(x => x.HostType.HasFlag(hostType))
                    .FirstOrDefault();
                if (extension != null)
                {
                    if (context.Container != null)
                    {
                        // Translate file path back from container path
                        file = context.Container.TranslateToHostPath(file);
                        command.Properties[IssueCommandProperties.File] = file;
                    }

                    // Get the values that represent the server path given a local path
                    string repoName;
                    string relativeSourcePath;
                    extension.ConvertLocalPath(context, file, out repoName, out relativeSourcePath);

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
