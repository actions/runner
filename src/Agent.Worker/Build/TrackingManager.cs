using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    [ServiceLocator(Default = typeof(TrackingManager))]
    public interface ITrackingManager : IAgentService
    {
        TrackingConfig Create(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            string hashKey,
            string file);

        TrackingConfigBase LoadIfExists(IExecutionContext executionContext, string file);

        void MarkForGarbageCollection(IExecutionContext executionContext, TrackingConfigBase config);

        void UpdateJobRunProperties(IExecutionContext executionContext, TrackingConfig config, string file);
    }

    public sealed class TrackingManager : AgentService, ITrackingManager
    {
        public TrackingConfig Create(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            string hashKey,
            string file)
        {
            Trace.Entering();

            // Get or create the top-level tracking config.
            TopLevelTrackingConfig topLevelConfig;
            string topLevelFile = Path.Combine(
                IOUtil.GetWorkPath(HostContext),
                Constants.Build.Path.SourceRootMappingDirectory,
                Constants.Build.Path.TopLevelTrackingConfigFile);
            Trace.Verbose($"Loading top-level tracking config if exists: {topLevelFile}");
            if (!File.Exists(topLevelFile))
            {
                topLevelConfig = new TopLevelTrackingConfig();
            }
            else
            {
                topLevelConfig = JsonConvert.DeserializeObject<TopLevelTrackingConfig>(
                    value: File.ReadAllText(topLevelFile));
            }

            // Update the top-level tracking config.
            topLevelConfig.LastBuildDirectoryCreatedOn = DateTimeOffset.Now;
            topLevelConfig.LastBuildDirectoryNumber++;
            WriteToFile(topLevelFile, topLevelConfig);

            // Create the new tracking config.
            TrackingConfig config = new TrackingConfig(
                executionContext,
                endpoint,
                topLevelConfig.LastBuildDirectoryNumber,
                hashKey);
            WriteToFile(file, config);
            return config;
        }

        public TrackingConfigBase LoadIfExists(IExecutionContext executionContext, string file)
        {
            Trace.Entering();

            // The tracking config will not exist for a new definition.
            if (!File.Exists(file))
            {
                return null;
            }

            // Load the content and distinguish between tracking config file
            // version 1 and file version 2.
            string content = File.ReadAllText(file);
            string fileFormatVersionJsonProperty = StringUtil.Format(
                @"""{0}""",
                TrackingConfig.FileFormatVersionJsonProperty);
            if (content.Contains(fileFormatVersionJsonProperty))
            {
                // The config is the new format.
                Trace.Verbose("Parsing new tracking config format.");
                return JsonConvert.DeserializeObject<TrackingConfig>(content);
            }

            // Attempt to parse the legacy format.
            Trace.Verbose("Parsing legacy tracking config format.");
            LegacyTrackingConfig config = LegacyTrackingConfig.TryParse(content);
            if (config == null)
            {
                executionContext.Warning(StringUtil.Loc("UnableToParseBuildTrackingConfig0", content));
            }

            return config;
        }

        public void MarkForGarbageCollection(IExecutionContext executionContext, TrackingConfigBase config)
        {
            Trace.Entering();

            // Convert legacy format to the new format.
            LegacyTrackingConfig legacyConfig = config as LegacyTrackingConfig;
            if (legacyConfig != null)
            {
                // Convert legacy format to the new format.
                config = new TrackingConfig(
                    executionContext,
                    legacyConfig,
                    // The sources folder wasn't stored in the legacy format - only the
                    // build folder was stored. Since the hash key has changed, it is
                    // unknown what the source folder was named. Just set the folder name
                    // to "s" so the property isn't left blank.
                    sourcesDirectoryNameOnly: Constants.Build.Path.SourcesDirectory);
            }

            // Write a copy of the tracking config to the GC folder.
            string gcDirectory = Path.Combine(
                IOUtil.GetWorkPath(HostContext),
                Constants.Build.Path.SourceRootMappingDirectory,
                Constants.Build.Path.GarbageCollectionDirectory);
            string file = Path.Combine(
                gcDirectory,
                StringUtil.Format("{0}.json", Guid.NewGuid()));
            WriteToFile(file, config);
        }

        public void UpdateJobRunProperties(IExecutionContext executionContext, TrackingConfig config, string file)
        {
            Trace.Entering();

            // Update the info properties and save the file.
            config.UpdateJobRunProperties(executionContext);
            WriteToFile(file, config);
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