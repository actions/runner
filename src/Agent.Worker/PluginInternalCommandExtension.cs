using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public sealed class PluginInternalCommandExtension : AgentService, IWorkerCommandExtension
    {
        public Type ExtensionType => typeof(IWorkerCommandExtension);

        public string CommandArea => "plugininternal";

        public HostTypes SupportedHostTypes => HostTypes.Build;

        public void ProcessCommand(IExecutionContext context, Command command)
        {
            if (String.Equals(command.Event, WellKnownPluginInternalCommand.UpdateRepositoryPath, StringComparison.OrdinalIgnoreCase))
            {
                ProcessPluginInternalUpdateRepositoryPathCommand(context, command.Properties, command.Data);
            }
            else
            {
                throw new Exception(StringUtil.Loc("PluginInternalCommandNotFound", command.Event));
            }
        }

        private void ProcessPluginInternalUpdateRepositoryPathCommand(IExecutionContext context, Dictionary<string, string> eventProperties, string data)
        {
            String alias;
            if (!eventProperties.TryGetValue(PluginInternalUpdateRepositoryEventProperties.Alias, out alias) || String.IsNullOrEmpty(alias))
            {
                throw new Exception(StringUtil.Loc("MissingRepositoryAlias"));
            }

            var repository = context.Repositories.FirstOrDefault(x => string.Equals(x.Alias, alias, StringComparison.OrdinalIgnoreCase));
            if (repository == null)
            {
                throw new Exception(StringUtil.Loc("RepositoryNotExist"));
            }

            if (string.IsNullOrEmpty(data))
            {
                throw new Exception(StringUtil.Loc("MissingRepositoryPath"));
            }

            var currentPath = repository.Properties.Get<string>(RepositoryPropertyNames.Path);
            if (!string.Equals(data.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), IOUtil.FilePathStringComparison))
            {
                repository.Properties.Set<string>(RepositoryPropertyNames.Path, data.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                var directoryManager = HostContext.GetService<IBuildDirectoryManager>();
                var trackingConfig = directoryManager.UpdateDirectory(context, repository);

                // Set the directory variables.
                string _workDirectory = HostContext.GetDirectory(WellKnownDirectory.Work);
                context.SetVariable(Constants.Variables.System.DefaultWorkingDirectory, Path.Combine(_workDirectory, trackingConfig.SourcesDirectory), isFilePath: true);
                context.SetVariable(Constants.Variables.Build.SourcesDirectory, Path.Combine(_workDirectory, trackingConfig.SourcesDirectory), isFilePath: true);
                context.SetVariable(Constants.Variables.Build.RepoLocalPath, Path.Combine(_workDirectory, trackingConfig.SourcesDirectory), isFilePath: true);
            }

            repository.Properties.Set("__AZP_READY", bool.TrueString);
        }
    }

    internal static class WellKnownPluginInternalCommand
    {
        public static readonly String UpdateRepositoryPath = "updaterepositorypath";
    }

    internal static class PluginInternalUpdateRepositoryEventProperties
    {
        public static readonly String Alias = "alias";
    }
}