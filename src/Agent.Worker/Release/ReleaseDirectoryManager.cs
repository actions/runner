using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Agent.Worker.Maintenance;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    public sealed class ReleaseDirectoryManager : AgentService, IReleaseDirectoryManager, IMaintenanceServiceProvider
    {
        public string MaintenanceDescription => StringUtil.Loc("DeleteUnusedReleaseDir");
        public Type ExtensionType => typeof(IMaintenanceServiceProvider);

        public ReleaseTrackingConfig PrepareArtifactsDirectory(
            string workingDirectory,
            string collectionId,
            string projectId,
            string releaseDefinition)
        {
            Trace.Entering();

            ArgUtil.NotNull(workingDirectory, nameof(workingDirectory));
            ArgUtil.NotNull(collectionId, nameof(collectionId));
            ArgUtil.NotNull(projectId, nameof(projectId));
            ArgUtil.NotNull(releaseDefinition, nameof(releaseDefinition));

            ReleaseTrackingConfig trackingConfig;
            string trackingConfigFile = Path.Combine(
                workingDirectory,
                Constants.Release.Path.RootMappingDirectory,
                collectionId,
                projectId,
                releaseDefinition,
                Constants.Release.Path.DefinitionMapping);

            Trace.Verbose($"Mappings file: {trackingConfigFile}");
            trackingConfig = LoadIfExists(trackingConfigFile);
            if (trackingConfig == null || trackingConfig.LastRunOn == null)
            {
                Trace.Verbose("Mappings file does not exist or in older format. A new mapping file will be created");
                var releaseDirectorySuffix = ComputeFolderInteger(workingDirectory);
                trackingConfig = new ReleaseTrackingConfig();
                trackingConfig.ReleaseDirectory = string.Format(
                    "{0}{1}",
                    Constants.Release.Path.ReleaseDirectoryPrefix,
                    releaseDirectorySuffix);
                trackingConfig.UpdateJobRunProperties();
                WriteToFile(trackingConfigFile, trackingConfig);
                Trace.Verbose($"Created a new mapping file: {trackingConfigFile}");
            }

            return trackingConfig;
        }

        public async Task RunMaintenanceOperation(IExecutionContext executionContext)
        {
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));

            var trackingManager = HostContext.GetService<IReleaseTrackingManager>();
            int staleReleaseDirThreshold = executionContext.Variables.GetInt("maintenance.deleteworkingdirectory.daysthreshold") ?? 0;
            if (staleReleaseDirThreshold > 0)
            {
                // scan unused Release directories
                executionContext.Output(StringUtil.Loc("DiscoverReleaseDir", staleReleaseDirThreshold));
                trackingManager.MarkExpiredForGarbageCollection(executionContext, TimeSpan.FromDays(staleReleaseDirThreshold));
            }
            else
            {
                executionContext.Output(StringUtil.Loc("GCReleaseDirNotEnabled"));
                return;
            }

            executionContext.Output(StringUtil.Loc("GCReleaseDir"));

            // delete unused Release directories
            trackingManager.DisposeCollectedGarbage(executionContext);
        }

        private int ComputeFolderInteger(string workingDirectory)
        {
            Trace.Entering();
            if (Directory.Exists(workingDirectory))
            {
                Regex regex = new Regex(string.Format(@"^{0}[0-9]*$", Constants.Release.Path.ReleaseDirectoryPrefix));
                var dirs = Directory.GetDirectories(workingDirectory);
                var folderNames = dirs.Select(Path.GetFileName).Where(name => regex.IsMatch(name));
                Trace.Verbose($"Number of folder with integer names: {folderNames.Count()}");

                if (folderNames.Any())
                {
                    var max = folderNames.Select(x => Int32.Parse(x.Substring(1))).Max();
                    return max + 1;
                }
            }

            return 1;
        }

        private ReleaseTrackingConfig LoadIfExists(string mappingFile)
        {
            Trace.Entering();
            Trace.Verbose($"Loading mapping file: {mappingFile}");
            if (!File.Exists(mappingFile))
            {
                return null;
            }

            string content = File.ReadAllText(mappingFile);
            var trackingConfig = JsonConvert.DeserializeObject<ReleaseTrackingConfig>(content);
            return trackingConfig;
        }

        private void WriteToFile(string file, object value)
        {
            Trace.Entering();
            Trace.Verbose($"Writing config to file: {file}");

            // Create the directory if it does not exist.
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            IOUtil.SaveObject(value, file);
        }
    }
}