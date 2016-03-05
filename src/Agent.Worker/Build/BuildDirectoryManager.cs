using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    [ServiceLocator(Default = typeof(BuildDirectoryManager))]
    public interface IBuildDirectoryManager : IAgentService
    {
        TrackingConfig PrepareDirectory(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            ISourceProvider sourceProvider);
    }

    public sealed class BuildDirectoryManager : AgentService, IBuildDirectoryManager
    {
        public TrackingConfig PrepareDirectory(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            ISourceProvider sourceProvider)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(endpoint, nameof(endpoint));
            ArgUtil.NotNull(sourceProvider, nameof(sourceProvider));
            var trackingManager = HostContext.GetService<ITrackingManager>();

            // Defer to the source provider to calculate the hash key.
            Trace.Verbose("Calculating build directory hash key.");
            string hashKey = sourceProvider.GetBuildDirectoryHashKey(executionContext, endpoint);

            // Load the existing tracking file if one already exists.
            string directory = Path.Combine(
                IOUtil.GetWorkPath(HostContext),
                Constants.Build.Path.SourceRootMappingDirectory,
                executionContext.Variables.System_CollectionId,
                executionContext.Variables.System_DefinitionId);
            string file = Path.Combine(
                directory,
                Constants.Build.Path.TrackingConfigFile);
            Trace.Verbose($"Loading tracking config if exists: {file}");
            TrackingConfigBase existingConfig = trackingManager.LoadIfExists(executionContext, file);

            // Check if the build needs to be garbage collected. If the hash key
            // has changed, then the existing build directory cannot be reused.
            TrackingConfigBase garbageConfig = null;
            if (existingConfig != null
                && !string.Equals(existingConfig.HashKey, hashKey, StringComparison.OrdinalIgnoreCase))
            {
                // Just store a reference to the config for now. It can safely be
                // marked for garbage collection only after the new build directory
                // config has been created.
                garbageConfig = existingConfig;
                existingConfig = null;
            }

            // Create a new tracking config if required.
            TrackingConfig newConfig;
            if (existingConfig == null)
            {
                newConfig = trackingManager.Create(executionContext, endpoint, hashKey, file);
            }
            else
            {
                // Convert legacy format to the new format if required.
                newConfig = ConvertToNewFormat(executionContext, endpoint, existingConfig);

                // For existing tracking config files, update the job run properties.
                trackingManager.UpdateJobRunProperties(executionContext, newConfig, file);
            }

            // Mark the old configuration for garbage collection.
            if (garbageConfig != null)
            {
                trackingManager.MarkForGarbageCollection(executionContext, garbageConfig);
            }

            // TODO: IMPLEMENT CODE DEALING WITH BuildCleanOption ENUM
            // // // manage built-in directories.
            // // // delete entire build directory if clean=all is set.
            // // // always recreate artifactstaging dir and testresult dir.
            // // // recreate binaries dir if clean=binary is set.
            // // // delete source dir if clean=src is set.
            // // if (cleanOpt == BuildCleanOption.All)
            // // {
            // //     DeleteDirectory(
            // //         executionContext,
            // //         description: "build directory",
            // //         path: Path.Combine(IOUtil.GetWorkPath(HostContext), newConfig.BuildDirectory));
            // // }

            CreateDirectory(
                executionContext,
                description: "artifacts directory",
                path: Path.Combine(IOUtil.GetWorkPath(HostContext), newConfig.ArtifactsDirectory));
            CreateDirectory(
                executionContext,
                description: "test results directory",
                path: Path.Combine(IOUtil.GetWorkPath(HostContext), newConfig.TestResultsDirectory));
            // TODO: IMPLEMENT CODE DEALING WITH BuildCleanOption ENUM
            // // CreateDirectory(
            // //     executionContext,
            // //     description: "binaries directory",
            // //     path: Path.Combine(IOUtil.GetWorkPath(HostContext), newConfig.BuildDirectory, Constants.Build.Path.BinariesDirectory),
            // //     deleteExistingFirst: cleanOpt == BuildCleanOption.Binary);
            // // if (cleanOpt == BuildCleanOption.Source)
            // // {
            // //     DeleteDirectory(
            // //         executionContext,
            // //         description: "source directory",
            // //         path: Path.Combine(IOUtil.GetWorkPath(HostContext), newConfig.BuildDirectory, Constants.Build.Path.SourcesDirectory));
            // // }

            return newConfig;
        }

        private TrackingConfig ConvertToNewFormat(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            TrackingConfigBase config)
        {
            Trace.Entering();

            // If it's already in the new format, return it.
            TrackingConfig newConfig = config as TrackingConfig;
            if (newConfig != null)
            {
                return newConfig;
            }

            // Delete the legacy artifact/staging directories.
            LegacyTrackingConfig legacyConfig = config as LegacyTrackingConfig;
            DeleteDirectory(
                executionContext,
                description: "legacy artifacts directory",
                path: Path.Combine(legacyConfig.BuildDirectory, Constants.Build.Path.LegacyArtifactsDirectory));
            DeleteDirectory(
                executionContext,
                description: "legacy staging directory",
                path: Path.Combine(legacyConfig.BuildDirectory, Constants.Build.Path.LegacyStagingDirectory));

            // Determine the source directory name. Check if the directory is named "s" already.
            // Convert the source directory to be named "s" if there is a problem with the old name.
            String sourcesDirectoryNameOnly = Constants.Build.Path.SourcesDirectory;
            if (!Directory.Exists(Path.Combine(legacyConfig.BuildDirectory, sourcesDirectoryNameOnly))
                && !String.Equals(endpoint.Name, Constants.Build.Path.ArtifactsDirectory, StringComparison.OrdinalIgnoreCase)
                && !String.Equals(endpoint.Name, Constants.Build.Path.LegacyArtifactsDirectory, StringComparison.OrdinalIgnoreCase)
                && !String.Equals(endpoint.Name, Constants.Build.Path.LegacyStagingDirectory, StringComparison.OrdinalIgnoreCase)
                && !String.Equals(endpoint.Name, Constants.Build.Path.TestResultsDirectory, StringComparison.OrdinalIgnoreCase)
                && !endpoint.Name.Contains("\\")
                && !endpoint.Name.Contains("/")
                && Directory.Exists(Path.Combine(legacyConfig.BuildDirectory, endpoint.Name)))
            {
                sourcesDirectoryNameOnly = endpoint.Name;
            }

            // Convert to the new format.
            newConfig = new TrackingConfig(
                executionContext,
                legacyConfig,
                sourcesDirectoryNameOnly,
                // The legacy artifacts directory has been deleted at this point - see above - so
                // switch the configuration to using the new naming scheme.
                useNewArtifactsDirectoryName: true);
            return newConfig;
        }

        private void CreateDirectory(IExecutionContext executionContext, string description, string path, bool deleteExisting = true)
        {
            // Delete.
            if (deleteExisting)
            {
                DeleteDirectory(executionContext, description, path);
            }

            // Create.
            if (!Directory.Exists(path))
            {
                Trace.Verbose($"Creating {description}.");
                Directory.CreateDirectory(path);
            }
        }

        private void DeleteDirectory(IExecutionContext executionContext, string description, string path)
        {
            Trace.Verbose($"Checking if {description} exists: '{path}'");
            if (Directory.Exists(path))
            {
                // Delete the files.
                executionContext.Debug($"Deleting {description}: '{path}'");
                Trace.Verbose("Deleting files.");
                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    executionContext.CancellationToken.ThrowIfCancellationRequested();
                    File.Delete(file);
                }

                // Delete the directories.
                Trace.Verbose("Deleting directories.");
                foreach (string directory in Directory.GetDirectories(path, "*", SearchOption.AllDirectories).OrderByDescending(x => x.Length))
                {
                    executionContext.CancellationToken.ThrowIfCancellationRequested();
                    Directory.Delete(directory);
                }

                Directory.Delete(path);
            }
        }
    }
}