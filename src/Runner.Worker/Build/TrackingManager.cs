using GitHub.DistributedTask.WebApi;
using Runner.Common.Util;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;
using GitHub.DistributedTask.Pipelines;

namespace Runner.Common.Worker.Build
{
    [ServiceLocator(Default = typeof(TrackingManager))]
    public interface ITrackingManager : IAgentService
    {
        TrackingConfig Create(
            IExecutionContext executionContext,
            RepositoryResource repository,
            string hashKey,
            string file,
            bool overrideBuildDirectory);

        TrackingConfig LoadIfExists(IExecutionContext executionContext, string file);

        void MarkForGarbageCollection(IExecutionContext executionContext, TrackingConfig config);

        void UpdateJobRunProperties(IExecutionContext executionContext, TrackingConfig config, string file);

        void MarkExpiredForGarbageCollection(IExecutionContext executionContext, TimeSpan expiration);

        void DisposeCollectedGarbage(IExecutionContext executionContext);

        void MaintenanceStarted(TrackingConfig config, string file);

        void MaintenanceCompleted(TrackingConfig config, string file);
    }

    public sealed class TrackingManager : AgentService, ITrackingManager
    {
        public TrackingConfig Create(
            IExecutionContext executionContext,
            RepositoryResource repository,
            string hashKey,
            string file,
            bool overrideBuildDirectory)
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
                            lastBuildNumber > topLevelConfig.LastBuildDirectoryNumber)
                        {
                            topLevelConfig.LastBuildDirectoryNumber = lastBuildNumber;
                        }
                    }
                }
            }

            // Determine the build directory.
            if (overrideBuildDirectory)
            {
                // This should only occur during hosted builds. This was added due to TFVC.
                // TFVC does not allow a local path for a single machine to be mapped in multiple
                // workspaces. The machine name for a hosted images is not unique.
                //
                // So if a customer is running two hosted builds at the same time, they could run
                // into the local mapping conflict.
                //
                // The workaround is to force the build directory to be different across all concurrent
                // hosted builds (for TFVC). The agent ID will be unique across all concurrent hosted
                // builds so that can safely be used as the build directory.
                ArgUtil.Equal(default(int), topLevelConfig.LastBuildDirectoryNumber, nameof(topLevelConfig.LastBuildDirectoryNumber));
                var configurationStore = HostContext.GetService<IConfigurationStore>();
                AgentSettings settings = configurationStore.GetSettings();
                topLevelConfig.LastBuildDirectoryNumber = settings.AgentId;
            }
            else
            {
                topLevelConfig.LastBuildDirectoryNumber++;
            }

            // Update the top-level tracking config.
            topLevelConfig.LastBuildDirectoryCreatedOn = DateTimeOffset.Now;
            WriteToFile(topLevelFile, topLevelConfig);

            // Create the new tracking config.
            TrackingConfig config = new TrackingConfig(
                executionContext,
                repository,
                topLevelConfig.LastBuildDirectoryNumber,
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

        public void MaintenanceStarted(TrackingConfig config, string file)
        {
            Trace.Entering();
            config.LastMaintenanceAttemptedOn = DateTimeOffset.Now;
            config.LastMaintenanceCompletedOn = null;
            WriteToFile(file, config);
        }

        public void MaintenanceCompleted(TrackingConfig config, string file)
        {
            Trace.Entering();
            config.LastMaintenanceCompletedOn = DateTimeOffset.Now;
            WriteToFile(file, config);
        }

        public void MarkExpiredForGarbageCollection(IExecutionContext executionContext, TimeSpan expiration)
        {
            Trace.Entering();
            Trace.Info("Scan all SourceFolder tracking files.");
            string searchRoot = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Build.Path.SourceRootMappingDirectory);
            if (!Directory.Exists(searchRoot))
            {
                executionContext.Output(StringUtil.Loc("GCDirNotExist", searchRoot));
                return;
            }

            var allTrackingFiles = Directory.EnumerateFiles(searchRoot, Constants.Build.Path.TrackingConfigFile, SearchOption.AllDirectories);
            Trace.Verbose($"Find {allTrackingFiles.Count()} tracking files.");

            executionContext.Output(StringUtil.Loc("DirExpireLimit", expiration.TotalDays));
            executionContext.Output(StringUtil.Loc("CurrentUTC", DateTime.UtcNow.ToString("o")));

            // scan all sourcefolder tracking file, find which folder has never been used since UTC-expiration
            // the scan and garbage discovery should be best effort.
            // if the tracking file is in old format, just delete the folder since the first time the folder been use we will convert the tracking file to new format.
            foreach (var trackingFile in allTrackingFiles)
            {
                try
                {
                    executionContext.Output(StringUtil.Loc("EvaluateTrackingFile", trackingFile));
                    TrackingConfig tracking = LoadIfExists(executionContext, trackingFile);
                    Trace.Verbose($"{trackingFile} is a new format tracking file.");
                    ArgUtil.NotNull(tracking.LastRunOn, nameof(tracking.LastRunOn));
                    executionContext.Output(StringUtil.Loc("BuildDirLastUseTIme", Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), tracking.BuildDirectory), tracking.LastRunOnString));
                    if (DateTime.UtcNow - expiration > tracking.LastRunOn)
                    {
                        executionContext.Output(StringUtil.Loc("GCUnusedTrackingFile", trackingFile, expiration.TotalDays));
                        MarkForGarbageCollection(executionContext, tracking);
                        IOUtil.DeleteFile(trackingFile);
                    }
                }
                catch (Exception ex)
                {
                    executionContext.Error(StringUtil.Loc("ErrorDuringBuildGC", trackingFile));
                    executionContext.Error(ex);
                }
            }
        }

        public void DisposeCollectedGarbage(IExecutionContext executionContext)
        {
            Trace.Entering();
            PrintOutDiskUsage(executionContext);

            string gcDirectory = Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Work),
                Constants.Build.Path.SourceRootMappingDirectory,
                Constants.Build.Path.GarbageCollectionDirectory);

            if (!Directory.Exists(gcDirectory))
            {
                executionContext.Output(StringUtil.Loc("GCDirNotExist", gcDirectory));
                return;
            }

            IEnumerable<string> gcTrackingFiles = Directory.EnumerateFiles(gcDirectory, "*.json");
            if (gcTrackingFiles == null || gcTrackingFiles.Count() == 0)
            {
                executionContext.Output(StringUtil.Loc("GCDirIsEmpty", gcDirectory));
                return;
            }

            Trace.Info($"Find {gcTrackingFiles.Count()} GC tracking files.");

            if (gcTrackingFiles.Count() > 0)
            {
                foreach (string gcFile in gcTrackingFiles)
                {
                    // maintenance has been cancelled.
                    executionContext.CancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var gcConfig = LoadIfExists(executionContext, gcFile) as TrackingConfig;
                        ArgUtil.NotNull(gcConfig, nameof(TrackingConfig));

                        string fullPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), gcConfig.BuildDirectory);
                        executionContext.Output(StringUtil.Loc("Deleting", fullPath));
                        IOUtil.DeleteDirectory(fullPath, executionContext.CancellationToken);

                        executionContext.Output(StringUtil.Loc("DeleteGCTrackingFile", fullPath));
                        IOUtil.DeleteFile(gcFile);
                    }
                    catch (Exception ex)
                    {
                        executionContext.Error(StringUtil.Loc("ErrorDuringBuildGCDelete", gcFile));
                        executionContext.Error(ex);
                    }
                }

                PrintOutDiskUsage(executionContext);
            }
        }

        private void PrintOutDiskUsage(IExecutionContext context)
        {
            // Print disk usage should be best effort, since DriveInfo can't detect usage of UNC share.
            try
            {
                context.Output($"Disk usage for working directory: {HostContext.GetDirectory(WellKnownDirectory.Work)}");
                var workDirectoryDrive = new DriveInfo(HostContext.GetDirectory(WellKnownDirectory.Work));
                long freeSpace = workDirectoryDrive.AvailableFreeSpace;
                long totalSpace = workDirectoryDrive.TotalSize;
#if OS_WINDOWS
                context.Output($"Working directory belongs to drive: '{workDirectoryDrive.Name}'");
#else
                context.Output($"Information about file system on which working directory resides.");
#endif
                context.Output($"Total size: '{totalSpace / 1024.0 / 1024.0} MB'");
                context.Output($"Available space: '{freeSpace / 1024.0 / 1024.0} MB'");
            }
            catch (Exception ex)
            {
                context.Warning($"Unable inspect disk usage for working directory {HostContext.GetDirectory(WellKnownDirectory.Work)}.");
                Trace.Error(ex);
                context.Debug(ex.ToString());
            }
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
