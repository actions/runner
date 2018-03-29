using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Release
{
    [ServiceLocator(Default = typeof(ReleaseTrackingManager))]
    public interface IReleaseTrackingManager : IAgentService
    {
        ReleaseTrackingConfig LoadIfExists(IExecutionContext executionContext, string file);

        void MarkExpiredForGarbageCollection(IExecutionContext executionContext, TimeSpan expiration);

        void DisposeCollectedGarbage(IExecutionContext executionContext);
    }

    public sealed class ReleaseTrackingManager : AgentService, IReleaseTrackingManager
    {
        public ReleaseTrackingConfig LoadIfExists(IExecutionContext executionContext, string file)
        {
            Trace.Entering();

            // The tracking config will not exist for a new definition.
            if (!File.Exists(file))
            {
                return null;
            }

            string content = File.ReadAllText(file);

            return JsonConvert.DeserializeObject<ReleaseTrackingConfig>(content);
        }

        private void MarkForGarbageCollection(IExecutionContext executionContext, ReleaseTrackingConfig config)
        {
            Trace.Entering();

            // Write a copy of the tracking config to the GC folder.
            string gcDirectory = Path.Combine(
                IOUtil.GetWorkPath(HostContext),
                Constants.Release.Path.RootMappingDirectory,
                Constants.Release.Path.GarbageCollectionDirectory);
            string file = Path.Combine(
                gcDirectory,
                StringUtil.Format("{0}.json", Guid.NewGuid()));
            WriteToFile(file, config);
        }

        public void MarkExpiredForGarbageCollection(IExecutionContext executionContext, TimeSpan expiration)
        {
            Trace.Entering();
            Trace.Info("Scan all SourceFolder tracking files.");
            string searchRoot = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), Constants.Release.Path.RootMappingDirectory);
            if (!Directory.Exists(searchRoot))
            {
                executionContext.Output(StringUtil.Loc("GCDirNotExist", searchRoot));
                return;
            }

            var allTrackingFiles = Directory.EnumerateFiles(searchRoot, Constants.Release.Path.TrackingConfigFile, SearchOption.AllDirectories);
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
                    executionContext.Output(StringUtil.Loc("EvaluateReleaseTrackingFile", trackingFile));
                    ReleaseTrackingConfig tracking = LoadIfExists(executionContext, trackingFile);
                    
                    if (tracking.LastRunOn == null)
                    {
                        Trace.Verbose($"{trackingFile} is a old format tracking file.");

                        executionContext.Output(StringUtil.Loc("GCOldFormatTrackingFile", trackingFile));
                        MarkForGarbageCollection(executionContext, tracking);
                        IOUtil.DeleteFile(trackingFile);
                    }
                    else
                    {
                        Trace.Verbose($"{trackingFile} is a new format tracking file.");
                        ArgUtil.NotNull(tracking.LastRunOn, nameof(tracking.LastRunOn));
                        executionContext.Output(StringUtil.Loc("ReleaseDirLastUseTIme", Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), tracking.ReleaseDirectory), tracking.LastRunOnString));
                        if (DateTime.UtcNow - expiration > tracking.LastRunOn)
                        {
                            executionContext.Output(StringUtil.Loc("GCUnusedTrackingFile", trackingFile, expiration.TotalDays));
                            MarkForGarbageCollection(executionContext, tracking);
                            IOUtil.DeleteFile(trackingFile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    executionContext.Error(StringUtil.Loc("ErrorDuringReleaseGC", trackingFile));
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
                Constants.Release.Path.RootMappingDirectory,
                Constants.Release.Path.GarbageCollectionDirectory);

            if (!Directory.Exists(gcDirectory))
            {
                executionContext.Output(StringUtil.Loc("GCReleaseDirNotExist", gcDirectory));
                return;
            }

            IEnumerable<string> gcTrackingFiles = Directory.EnumerateFiles(gcDirectory, "*.json");
            if (gcTrackingFiles == null || gcTrackingFiles.Count() == 0)
            {
                executionContext.Output(StringUtil.Loc("GCReleaseDirIsEmpty", gcDirectory));
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
                        var gcConfig = LoadIfExists(executionContext, gcFile) as ReleaseTrackingConfig;
                        ArgUtil.NotNull(gcConfig, nameof(ReleaseTrackingConfig));

                        string fullPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), gcConfig.ReleaseDirectory);
                        executionContext.Output(StringUtil.Loc("Deleting", fullPath));
                        IOUtil.DeleteDirectory(fullPath, executionContext.CancellationToken);

                        executionContext.Output(StringUtil.Loc("DeleteGCTrackingFile", fullPath));
                        IOUtil.DeleteFile(gcFile);
                    }
                    catch (Exception ex)
                    {
                        executionContext.Error(StringUtil.Loc("ErrorDuringReleaseGCDelete", gcFile));
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