using GitHub.Runner.Common.Util;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using GitHub.DistributedTask.Pipelines;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GitHub.Runner.Common;
using GitHub.Runner.Sdk;

namespace GitHub.Runner.Worker.Build
{
    [ServiceLocator(Default = typeof(BuildDirectoryManager))]
    public interface IBuildDirectoryManager : IAgentService
    {
        TrackingConfig PrepareDirectory(
            IExecutionContext executionContext,
            RepositoryResource repository,
            WorkspaceOptions workspace);

        // void CreateDirectory(
        //     IExecutionContext executionContext,
        //     string description, string path,
        //     bool deleteExisting);

        TrackingConfig UpdateDirectory(
            IExecutionContext executionContext,
            RepositoryResource repository);
    }

    public sealed class BuildDirectoryManager : AgentService, IBuildDirectoryManager
    {
        public string MaintenanceDescription => StringUtil.Loc("DeleteUnusedBuildDir");

        public TrackingConfig PrepareDirectory(
            IExecutionContext executionContext,
            RepositoryResource repository,
            WorkspaceOptions workspace)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(executionContext.Variables, nameof(executionContext.Variables));
            ArgUtil.NotNull(repository, nameof(repository));
            var trackingManager = HostContext.GetService<ITrackingManager>();

            // Defer to the source provider to calculate the hash key.
            Trace.Verbose("Calculating build directory hash key.");
            string hashKey = GetSourceDirectoryHashKey(executionContext);
            Trace.Verbose($"Hash key: {hashKey}");

            // Load the existing tracking file if one already exists.
            string trackingFile = Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Work),
                Constants.Build.Path.SourceRootMappingDirectory,
                executionContext.Variables.System_CollectionId,
                executionContext.Variables.System_DefinitionId,
                Constants.Build.Path.TrackingConfigFile);
            Trace.Verbose($"Loading tracking config if exists: {trackingFile}");
            TrackingConfig existingConfig = trackingManager.LoadIfExists(executionContext, trackingFile);

            // Check if the build needs to be garbage collected. If the hash key
            // has changed, then the existing build directory cannot be reused.
            TrackingConfig garbageConfig = null;
            if (existingConfig != null
                && !string.Equals(existingConfig.HashKey, hashKey, StringComparison.OrdinalIgnoreCase))
            {
                // Just store a reference to the config for now. It can safely be
                // marked for garbage collection only after the new build directory
                // config has been created.
                Trace.Verbose($"Hash key from existing tracking config does not match. Existing key: {existingConfig.HashKey}");
                garbageConfig = existingConfig;
                existingConfig = null;
            }

            // Create a new tracking config if required.
            TrackingConfig newConfig;
            if (existingConfig == null)
            {
                Trace.Verbose("Creating a new tracking config file.");
                var agentSetting = HostContext.GetService<IConfigurationStore>().GetSettings();
                newConfig = trackingManager.Create(
                    executionContext,
                    repository,
                    hashKey,
                    trackingFile,
                    false);
                // repository.TestOverrideBuildDirectory(agentSetting));
                ArgUtil.NotNull(newConfig, nameof(newConfig));
            }
            else
            {
                // Convert legacy format to the new format if required.
                newConfig = ConvertToNewFormat(executionContext, repository, existingConfig);

                // Fill out repository type if it's not there.
                // repository type is a new property introduced for maintenance job
                if (string.IsNullOrEmpty(newConfig.RepositoryType))
                {
                    newConfig.RepositoryType = repository.Type;
                }

                // For existing tracking config files, update the job run properties.
                Trace.Verbose("Updating job run properties.");
                trackingManager.UpdateJobRunProperties(executionContext, newConfig, trackingFile);
            }

            // Mark the old configuration for garbage collection.
            if (garbageConfig != null)
            {
                Trace.Verbose("Marking existing config for garbage collection.");
                trackingManager.MarkForGarbageCollection(executionContext, garbageConfig);
            }

            // Prepare the build directory.
            // There are 2 ways to provide build directory clean policy.
            //     1> set definition variable build.clean or agent.clean.buildDirectory. (on-prem user need to use this, since there is no Web UI in TFS 2016)
            //     2> select source clean option in definition repository tab. (VSTS will have this option in definition designer UI)
            BuildCleanOption cleanOption = GetBuildDirectoryCleanOption(executionContext, workspace);

            CreateDirectory(
                executionContext,
                description: "build directory",
                path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), newConfig.BuildDirectory),
                deleteExisting: cleanOption == BuildCleanOption.All);
            CreateDirectory(
                executionContext,
                description: "artifacts directory",
                path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), newConfig.ArtifactsDirectory),
                deleteExisting: true);
            CreateDirectory(
                executionContext,
                description: "binaries directory",
                path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), newConfig.BuildDirectory, Constants.Build.Path.BinariesDirectory),
                deleteExisting: cleanOption == BuildCleanOption.Binary);
            CreateDirectory(
                executionContext,
                description: "source directory",
                path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), newConfig.SourcesDirectory),
                deleteExisting: cleanOption == BuildCleanOption.Source);

            var repoPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), newConfig.SourcesDirectory);
            Trace.Info($"Set repository path for repository {repository.Alias} to '{repoPath}'");
            repository.Properties.Set<string>(RepositoryPropertyNames.Path, repoPath);

            return newConfig;
        }

        public TrackingConfig UpdateDirectory(
            IExecutionContext executionContext,
            RepositoryResource repository)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(executionContext.Variables, nameof(executionContext.Variables));
            ArgUtil.NotNull(repository, nameof(repository));
            var trackingManager = HostContext.GetService<ITrackingManager>();

            // Defer to the source provider to calculate the hash key.
            Trace.Verbose("Calculating build directory hash key.");
            string hashKey = GetSourceDirectoryHashKey(executionContext);
            Trace.Verbose($"Hash key: {hashKey}");

            // Load the existing tracking file.
            string trackingFile = Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Work),
                Constants.Build.Path.SourceRootMappingDirectory,
                executionContext.Variables.System_CollectionId,
                executionContext.Variables.System_DefinitionId,
                Constants.Build.Path.TrackingConfigFile);
            Trace.Verbose($"Loading tracking config if exists: {trackingFile}");
            TrackingConfig existingConfig = trackingManager.LoadIfExists(executionContext, trackingFile);
            ArgUtil.NotNull(existingConfig, nameof(existingConfig));

            TrackingConfig newConfig = ConvertToNewFormat(executionContext, repository, existingConfig);
            ArgUtil.NotNull(newConfig, nameof(newConfig));

            var repoPath = repository.Properties.Get<string>(RepositoryPropertyNames.Path);
            ArgUtil.NotNullOrEmpty(repoPath, nameof(repoPath));
            Trace.Info($"Update repository path for repository {repository.Alias} to '{repoPath}'");

            string buildDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), newConfig.BuildDirectory);
            if (repoPath.StartsWith(buildDirectory + Path.DirectorySeparatorChar) || repoPath.StartsWith(buildDirectory + Path.AltDirectorySeparatorChar))
            {
                // The sourcesDirectory in tracking file is a relative path to agent's work folder.
                newConfig.SourcesDirectory = repoPath.Substring(HostContext.GetDirectory(WellKnownDirectory.Work).Length + 1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else
            {
                throw new ArgumentException($"Repository path '{repoPath}' should be located under agent's work directory '{buildDirectory}'.");
            }

            // Update the tracking config files.
            Trace.Verbose("Updating job run properties.");
            trackingManager.UpdateJobRunProperties(executionContext, newConfig, trackingFile);

            return newConfig;
        }

        private string GetSourceDirectoryHashKey(IExecutionContext executionContext)
        {
            // Validate parameters.
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(executionContext.Variables, nameof(executionContext.Variables));

            // Calculate the hash key.
            const string Format = "{{{{ \r\n    \"system\" : \"github\", \r\n    \"collectionId\" = \"{0}\", \r\n    \"definitionId\" = \"{1}\"\r\n}}}}";
            string hashInput = string.Format(
                CultureInfo.InvariantCulture,
                Format,
                executionContext.Variables.System_CollectionId,
                executionContext.Variables.System_DefinitionId);
            using (SHA1 sha1Hash = SHA1.Create())
            {
                byte[] data = sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
                StringBuilder hexString = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    hexString.Append(data[i].ToString("x2"));
                }

                return hexString.ToString();
            }
        }

        private TrackingConfig ConvertToNewFormat(
            IExecutionContext executionContext,
            RepositoryResource repository,
            TrackingConfig config)
        {
            Trace.Entering();

            // If it's already in the new format, return it.
            TrackingConfig newConfig = config as TrackingConfig;
            ArgUtil.NotNull(newConfig, nameof(newConfig));
            // if (newConfig != null)
            // {
            return newConfig;
            // }

            // Delete the legacy artifact/staging directories.
            // LegacyTrackingConfig legacyConfig = config as LegacyTrackingConfig;
            // DeleteDirectory(
            //     executionContext,
            //     description: "legacy artifacts directory",
            //     path: Path.Combine(legacyConfig.BuildDirectory, Constants.Build.Path.LegacyArtifactsDirectory));
            // DeleteDirectory(
            //     executionContext,
            //     description: "legacy staging directory",
            //     path: Path.Combine(legacyConfig.BuildDirectory, Constants.Build.Path.LegacyStagingDirectory));

            // // Determine the source directory name. Check if the directory is named "s" already.
            // // Convert the source directory to be named "s" if there is a problem with the old name.
            // string sourcesDirectoryNameOnly = Constants.Build.Path.SourcesDirectory;
            // string repositoryName = repository.Properties.Get<string>(RepositoryPropertyNames.Name);
            // if (!Directory.Exists(Path.Combine(legacyConfig.BuildDirectory, sourcesDirectoryNameOnly))
            //     && !String.Equals(repositoryName, Constants.Build.Path.ArtifactsDirectory, StringComparison.OrdinalIgnoreCase)
            //     && !String.Equals(repositoryName, Constants.Build.Path.LegacyArtifactsDirectory, StringComparison.OrdinalIgnoreCase)
            //     && !String.Equals(repositoryName, Constants.Build.Path.LegacyStagingDirectory, StringComparison.OrdinalIgnoreCase)
            //     && !String.Equals(repositoryName, Constants.Build.Path.TestResultsDirectory, StringComparison.OrdinalIgnoreCase)
            //     && !repositoryName.Contains("\\")
            //     && !repositoryName.Contains("/")
            //     && Directory.Exists(Path.Combine(legacyConfig.BuildDirectory, repositoryName)))
            // {
            //     sourcesDirectoryNameOnly = repositoryName;
            // }

            // // Convert to the new format.
            // newConfig = new TrackingConfig(
            //     executionContext,
            //     legacyConfig,
            //     sourcesDirectoryNameOnly,
            //     repository.Type,
            //     // The legacy artifacts directory has been deleted at this point - see above - so
            //     // switch the configuration to using the new naming scheme.
            //     useNewArtifactsDirectoryName: true);
            // return newConfig;
        }

        public void CreateDirectory(IExecutionContext executionContext, string description, string path, bool deleteExisting)
        {
            // Delete.
            if (deleteExisting)
            {
                executionContext.Debug($"Delete existing {description}: '{path}'");
                DeleteDirectory(executionContext, description, path);
            }

            // Create.
            if (!Directory.Exists(path))
            {
                executionContext.Debug($"Creating {description}: '{path}'");
                Trace.Info($"Creating {description}.");
                Directory.CreateDirectory(path);
            }
        }

        private void DeleteDirectory(IExecutionContext executionContext, string description, string path)
        {
            Trace.Info($"Checking if {description} exists: '{path}'");
            if (Directory.Exists(path))
            {
                executionContext.Debug($"Deleting {description}: '{path}'");
                IOUtil.DeleteDirectory(path, executionContext.CancellationToken);
            }
        }

        // Prefer variable over endpoint data when get build directory clean option.
        // Prefer agent.clean.builddirectory over build.clean when use variable
        // available value for build.clean or agent.clean.builddirectory:
        //      Delete entire build directory if build.clean=all is set.
        //      Recreate binaries dir if clean=binary is set.
        //      Recreate source dir if clean=src is set.
        private BuildCleanOption GetBuildDirectoryCleanOption(IExecutionContext executionContext, WorkspaceOptions workspace)
        {
            BuildCleanOption? cleanOption = null; //executionContext.Variables.Build_Clean;
            if (cleanOption != null)
            {
                return cleanOption.Value;
            }

            if (workspace == null)
            {
                return BuildCleanOption.None;
            }
            else
            {
                // Dictionary<string, string> workspaceClean = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                // workspaceClean["clean"] = workspace.Clean;
                // executionContext.Variables.ExpandValues(target: workspaceClean);
                // VarUtil.ExpandEnvironmentVariables(HostContext, target: workspaceClean);
                string expandedClean = workspace.Clean; //workspaceClean["clean"];
                if (string.Equals(expandedClean, PipelineConstants.WorkspaceCleanOptions.All, StringComparison.OrdinalIgnoreCase))
                {
                    return BuildCleanOption.All;
                }
                else if (string.Equals(expandedClean, PipelineConstants.WorkspaceCleanOptions.Resources, StringComparison.OrdinalIgnoreCase))
                {
                    return BuildCleanOption.Source;
                }
                else if (string.Equals(expandedClean, PipelineConstants.WorkspaceCleanOptions.Outputs, StringComparison.OrdinalIgnoreCase))
                {
                    return BuildCleanOption.Binary;
                }
                else
                {
                    return BuildCleanOption.None;
                }
            }
        }
    }


    // TODO: use enum defined in build2.webapi when it's available.
    public enum RepositoryCleanOptions
    {
        Source,
        SourceAndOutput,
        SourceDir,
        AllBuildDir,
    }
}
