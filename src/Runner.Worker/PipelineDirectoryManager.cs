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

namespace GitHub.Runner.Worker
{
    [ServiceLocator(Default = typeof(PipelineDirectoryManager))]
    public interface IPipelineDirectoryManager : IRunnerService
    {
        TrackingConfig PrepareDirectory(
            IExecutionContext executionContext,
            WorkspaceOptions workspace);

        TrackingConfig UpdateDirectory(
            IExecutionContext executionContext);
    }

    public sealed class PipelineDirectoryManager : RunnerService, IPipelineDirectoryManager
    {
        public TrackingConfig PrepareDirectory(
            IExecutionContext executionContext,
            WorkspaceOptions workspace)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(executionContext.Variables, nameof(executionContext.Variables));
            var trackingManager = HostContext.GetService<ITrackingManager>();

            // Defer to the source provider to calculate the hash key.
            Trace.Verbose("Calculating pipeline directory hash key.");
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
            TrackingConfig trackingConfig = trackingManager.LoadIfExists(executionContext, trackingFile);

            // Check if the pipeline needs to be garbage collected. If the hash key
            // has changed, then the existing pipeline directory cannot be reused.
            TrackingConfig garbageConfig = null;
            if (trackingConfig != null
                && !string.Equals(trackingConfig.HashKey, hashKey, StringComparison.OrdinalIgnoreCase))
            {
                // Just store a reference to the config for now. It can safely be
                // marked for garbage collection only after the new pipeline directory
                // config has been created.
                Trace.Verbose($"Hash key from existing tracking config does not match. Existing key: {trackingConfig.HashKey}");
                garbageConfig = trackingConfig;
                trackingConfig = null;
            }

            // Create a new tracking config if required.
            if (trackingConfig == null)
            {
                Trace.Verbose("Creating a new tracking config file.");
                trackingConfig = trackingManager.Create(
                    executionContext,
                    hashKey,
                    trackingFile);
                ArgUtil.NotNull(trackingConfig, nameof(trackingConfig));
            }
            else
            {
                // For existing tracking config files, update the job run properties.
                Trace.Verbose("Updating job run properties.");
                trackingManager.UpdateJobRunProperties(executionContext, trackingConfig, trackingFile);
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
                path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.PipelineDirectory),
                deleteExisting: cleanOption == BuildCleanOption.All);
            CreateDirectory(
                executionContext,
                description: "artifacts directory",
                path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.ArtifactsDirectory),
                deleteExisting: true);
            CreateDirectory(
                executionContext,
                description: "binaries directory",
                path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.PipelineDirectory, Constants.Build.Path.BinariesDirectory),
                deleteExisting: cleanOption == BuildCleanOption.Binary);
            CreateDirectory(
                executionContext,
                description: "source directory",
                path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.SourcesDirectory),
                deleteExisting: cleanOption == BuildCleanOption.Source);

            return trackingConfig;
        }

        public TrackingConfig UpdateDirectory(
            IExecutionContext executionContext)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(executionContext.Variables, nameof(executionContext.Variables));
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

            var repoPath = executionContext.GetGitHubContext("workspace");
            ArgUtil.NotNullOrEmpty(repoPath, nameof(repoPath));
            Trace.Info($"Update workspace repository path to '{repoPath}'");

            string buildDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), existingConfig.PipelineDirectory);
            if (repoPath.StartsWith(buildDirectory + Path.DirectorySeparatorChar) || repoPath.StartsWith(buildDirectory + Path.AltDirectorySeparatorChar))
            {
                // The sourcesDirectory in tracking file is a relative path to agent's work folder.
                existingConfig.SourcesDirectory = repoPath.Substring(HostContext.GetDirectory(WellKnownDirectory.Work).Length + 1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else
            {
                throw new ArgumentException($"Repository path '{repoPath}' should be located under agent's work directory '{buildDirectory}'.");
            }

            // Update the tracking config files.
            Trace.Verbose("Updating job run properties.");
            trackingManager.UpdateJobRunProperties(executionContext, existingConfig, trackingFile);

            return existingConfig;
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
