using GitHub.DistributedTask.WebApi;
using GitHub.Runner.Common.Util;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;
using GitHub.DistributedTask.Pipelines;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(TrackingManager))]
    public interface ITrackingManager : IRunnerService
    {
        TrackingConfig Create(
            IExecutionContext executionContext,
            RepositoryResource repository,
            string hashKey,
            string file);

        TrackingConfig LoadIfExists(IExecutionContext executionContext, string file);

        void MarkForGarbageCollection(IExecutionContext executionContext, TrackingConfig config);

        void UpdateJobRunProperties(IExecutionContext executionContext, TrackingConfig config, string file);
    }

    public sealed class TrackingManager : RunnerService, ITrackingManager
    {
        public TrackingConfig Create(
            IExecutionContext executionContext,
            RepositoryResource repository,
            string hashKey,
            string file)
        {
            Trace.Entering();

            // Get or create the top-level tracking config.
            TopLevelTrackingConfig topLevelConfig;
            string topLevelFile = Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Work),
                Constants.Build.Path.SourceRootMappingDirectory,
                Constants.Build.Path.TopLevelTrackingConfigFile);
            Trace.Verbose($"Loading top-level tracking config if exists: {topLevelFile}");
            if (!File.Exists(topLevelFile))
            {
                topLevelConfig = new TopLevelTrackingConfig();
            }
            else
            {
                topLevelConfig = JsonConvert.DeserializeObject<TopLevelTrackingConfig>(File.ReadAllText(topLevelFile));
                if (topLevelConfig == null)
                {
                    executionContext.Warning($"Rebuild corruptted top-level tracking configure file {topLevelFile}.");
                    // save the corruptted file in case we need to investigate more.
                    File.Copy(topLevelFile, $"{topLevelFile}.corruptted", true);

                    topLevelConfig = new TopLevelTrackingConfig();
                    DirectoryInfo workDir = new DirectoryInfo(HostContext.GetDirectory(WellKnownDirectory.Work));

                    foreach (var dir in workDir.EnumerateDirectories())
                    {
                        // we scan the entire _work directory and find the directory with the highest integer number.
                        if (int.TryParse(dir.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lastBuildNumber) &&
                            lastBuildNumber > topLevelConfig.LastPipelineDirectoryNumber)
                        {
                            topLevelConfig.LastPipelineDirectoryNumber = lastBuildNumber;
                        }
                    }
                }
            }

            topLevelConfig.LastPipelineDirectoryNumber++;

            // Update the top-level tracking config.
            topLevelConfig.LastPipelineDirectoryCreatedOn = DateTimeOffset.Now;
            WriteToFile(topLevelFile, topLevelConfig);

            // Create the new tracking config.
            TrackingConfig config = new TrackingConfig(
                executionContext,
                repository,
                topLevelConfig.LastPipelineDirectoryNumber,
                hashKey);
            WriteToFile(file, config);
            return config;
        }

        public TrackingConfig LoadIfExists(IExecutionContext executionContext, string file)
        {
            Trace.Entering();

            // The tracking config will not exist for a new definition.
            if (!File.Exists(file))
            {
                return null;
            }

            return IOUtil.LoadObject<TrackingConfig>(file);
        }

        public void MarkForGarbageCollection(IExecutionContext executionContext, TrackingConfig config)
        {
            Trace.Entering();

            // Write a copy of the tracking config to the GC folder.
            string gcDirectory = Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Work),
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
