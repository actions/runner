using System;
using System.IO;
using System.Linq;
using GitHub.DistributedTask.Pipelines;
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

        TrackingConfig UpdateRepositoryDirectory(
            IExecutionContext executionContext,
            string repositoryFullName,
            string repositoryPath,
            bool workspaceRepository);
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
            var trackingManager = HostContext.GetService<ITrackingManager>();

            var repoFullName = executionContext.GetGitHubContext("repository");
            ArgUtil.NotNullOrEmpty(repoFullName, nameof(repoFullName));

            // Load the existing tracking file if one already exists.
            string trackingFile = Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Work),
                Constants.Pipeline.Path.PipelineMappingDirectory,
                repoFullName,
                Constants.Pipeline.Path.TrackingConfigFile);
            Trace.Info($"Loading tracking config if exists: {trackingFile}");
            TrackingConfig trackingConfig = trackingManager.LoadIfExists(executionContext, trackingFile);

            // Create a new tracking config if required.
            if (trackingConfig == null)
            {
                Trace.Info("Creating a new tracking config file.");
                trackingConfig = trackingManager.Create(
                    executionContext,
                    trackingFile);
                ArgUtil.NotNull(trackingConfig, nameof(trackingConfig));
            }
            else
            {
                // For existing tracking config files, update the job run properties.
                Trace.Info("Updating job run properties.");
                trackingConfig.LastRunOn = DateTimeOffset.Now;
                trackingManager.Update(executionContext, trackingConfig, trackingFile);
            }

            // Prepare the pipeline directory.
            if (string.Equals(workspace?.Clean, PipelineConstants.WorkspaceCleanOptions.All, StringComparison.OrdinalIgnoreCase))
            {
                CreateDirectory(
                    executionContext,
                    description: "pipeline directory",
                    path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.PipelineDirectory),
                    deleteExisting: true);

                CreateDirectory(
                    executionContext,
                    description: "workspace directory",
                    path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.WorkspaceDirectory),
                    deleteExisting: true);
            }
            else if (string.Equals(workspace?.Clean, PipelineConstants.WorkspaceCleanOptions.Resources, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var repository in trackingConfig.Repositories)
                {
                    CreateDirectory(
                        executionContext,
                        description: $"directory {repository.Value.RepositoryPath} for repository {repository.Key}",
                        path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), repository.Value.RepositoryPath),
                        deleteExisting: true);
                }
            }
            else if (string.Equals(workspace?.Clean, PipelineConstants.WorkspaceCleanOptions.Outputs, StringComparison.OrdinalIgnoreCase))
            {
                var allDirectories = Directory.GetDirectories(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.PipelineDirectory)).ToList();
                foreach (var repository in trackingConfig.Repositories)
                {
                    allDirectories.Remove(Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), repository.Value.RepositoryPath));
                }

                foreach (var deleteDir in allDirectories)
                {
                    executionContext.Debug($"Delete existing untracked directory '{deleteDir}'");
                    DeleteDirectory(executionContext, "untracked dir", deleteDir);
                }
            }
            else
            {
                CreateDirectory(
                    executionContext,
                    description: "pipeline directory",
                    path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.PipelineDirectory),
                    deleteExisting: false);

                CreateDirectory(
                    executionContext,
                    description: "workspace directory",
                    path: Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), trackingConfig.WorkspaceDirectory),
                    deleteExisting: false);
            }

            return trackingConfig;
        }

        public TrackingConfig UpdateRepositoryDirectory(
            IExecutionContext executionContext,
            string repositoryFullName,
            string repositoryPath,
            bool workspaceRepository)
        {
            // Validate parameters.
            Trace.Entering();
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNullOrEmpty(repositoryFullName, nameof(repositoryFullName));
            ArgUtil.NotNullOrEmpty(repositoryPath, nameof(repositoryPath));

            // we need the repository for the pipeline, since the tracking file is based on the workflow repository
            var pipelineRepoFullName = executionContext.GetGitHubContext("repository");
            ArgUtil.NotNullOrEmpty(pipelineRepoFullName, nameof(pipelineRepoFullName));

            // Load the existing tracking file.
            string trackingFile = Path.Combine(
                HostContext.GetDirectory(WellKnownDirectory.Work),
                Constants.Pipeline.Path.PipelineMappingDirectory,
                pipelineRepoFullName,
                Constants.Pipeline.Path.TrackingConfigFile);

            Trace.Verbose($"Loading tracking config if exists: {trackingFile}");
            var trackingManager = HostContext.GetService<ITrackingManager>();
            TrackingConfig existingConfig = trackingManager.LoadIfExists(executionContext, trackingFile);
            ArgUtil.NotNull(existingConfig, nameof(existingConfig));

            Trace.Info($"Update repository {repositoryFullName}'s path to '{repositoryPath}'");
            string pipelineDirectory = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Work), existingConfig.PipelineDirectory);
            if (repositoryPath.StartsWith(pipelineDirectory + Path.DirectorySeparatorChar) || repositoryPath.StartsWith(pipelineDirectory + Path.AltDirectorySeparatorChar))
            {
                // The workspaceDirectory in tracking file is a relative path to runner's pipeline directory.
                var repositoryRelativePath = repositoryPath.Substring(HostContext.GetDirectory(WellKnownDirectory.Work).Length + 1).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!existingConfig.Repositories.ContainsKey(repositoryFullName))
                {
                    existingConfig.Repositories[repositoryFullName] = new RepositoryTrackingConfig();
                }

                existingConfig.Repositories[repositoryFullName].RepositoryPath = repositoryRelativePath;
                existingConfig.Repositories[repositoryFullName].LastRunOn = DateTimeOffset.Now;

                if (workspaceRepository)
                {
                    Trace.Info($"Update workspace to '{repositoryPath}'");
                    existingConfig.WorkspaceDirectory = repositoryRelativePath;
                    executionContext.SetGitHubContext("workspace", repositoryPath);
                }

                // Update the tracking config files.
                Trace.Info("Updating repository tracking.");
                trackingManager.Update(executionContext, existingConfig, trackingFile);

                return existingConfig;
            }
            else
            {
                throw new ArgumentException($"Repository path '{repositoryPath}' should be located under runner's pipeline directory '{pipelineDirectory}'.");
            }
        }

        private void CreateDirectory(IExecutionContext executionContext, string description, string path, bool deleteExisting)
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
    }
}
