using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.IO;
using Microsoft.VisualStudio.Services.Agent.Worker.Build;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(TrackingManager2))]
    public interface ITrackingManager2 : IAgentService
    {
        TrackingConfig2 Create(IExecutionContext executionContext, string repositoryUrl, string hashKey, string file);

        TrackingConfigBase2 LoadIfExists(IExecutionContext executionContext, string file);

        void MarkForGarbageCollection(IExecutionContext executionContext, TrackingConfigBase2 config);

        void UpdateJobRunProperties(IExecutionContext executionContext, TrackingConfig2 config, string file);
    }

    public sealed class TrackingManager2 : AgentService, ITrackingManager2
    {
        public TrackingConfig2 Create(
            IExecutionContext executionContext,
            string repositoryUrl,
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
            TrackingConfig2 config = new TrackingConfig2(
                executionContext,
                repositoryUrl,
                topLevelConfig.LastBuildDirectoryNumber,
                hashKey);
            WriteToFile(file, config);
            return config;
        }

        public TrackingConfigBase2 LoadIfExists(IExecutionContext executionContext, string file)
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
                TrackingConfig2.FileFormatVersionJsonProperty);
            if (content.Contains(fileFormatVersionJsonProperty))
            {
                // The config is the new format.
                Trace.Verbose("Parsing new tracking config format.");
                return JsonConvert.DeserializeObject<TrackingConfig2>(content);
            }

            // Attempt to parse the legacy format.
            Trace.Verbose("Parsing legacy tracking config format.");
            LegacyTrackingConfig2 config = LegacyTrackingConfig2.TryParse(content);
            if (config == null)
            {
                executionContext.Warning(StringUtil.Loc("UnableToParseBuildTrackingConfig0", content));
            }

            return config;
        }

        public void MarkForGarbageCollection(IExecutionContext executionContext, TrackingConfigBase2 config)
        {
            Trace.Entering();

            // Convert legacy format to the new format.
            LegacyTrackingConfig2 legacyConfig = config as LegacyTrackingConfig2;
            if (legacyConfig != null)
            {
                // Convert legacy format to the new format.
                config = new TrackingConfig2(
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

        public void UpdateJobRunProperties(IExecutionContext executionContext, TrackingConfig2 config, string file)
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