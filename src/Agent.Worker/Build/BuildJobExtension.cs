using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class BuildJobExtension : AgentService, IJobExtension
    {
        public Type ExtensionType => typeof(IJobExtension);
        public string HostType => "build";
        public IStep PrepareStep { get; private set; }
        public IStep FinallyStep { get; private set; }
        private ServiceEndpoint SourceEndpoint { set; get; }
        private ISourceProvider SourceProvider { set; get; }

        public BuildJobExtension()
        {
            PrepareStep = new JobExtensionRunner(
                runAsync: PrepareAsync,
                alwaysRun: false,
                continueOnError: false,
                critical: true,
                displayName: StringUtil.Loc("GetSources"),
                enabled: true,
                @finally: false);

            FinallyStep = new JobExtensionRunner(
                runAsync: FinallyAsync,
                alwaysRun: false,
                continueOnError: false,
                critical: false,
                displayName: StringUtil.Loc("Cleanup"),
                enabled: true,
                @finally: true);
        }

        private async Task PrepareAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(PrepareStep, nameof(PrepareStep));
            ArgUtil.NotNull(PrepareStep.ExecutionContext, nameof(PrepareStep.ExecutionContext));
            IExecutionContext executionContext = PrepareStep.ExecutionContext;
            var directoryManager = HostContext.GetService<IBuildDirectoryManager>();

            // This flag can be false for jobs like cleanup artifacts.
            // If syncSources = false, we will not set source related build variable, not create build folder, not sync source.
            bool syncSources = executionContext.Variables.Build_SyncSources ?? true;
            if (!syncSources)
            {
                Trace.Info($"{Constants.Variables.Build.SyncSources} = false, we will not set source related build variable, not create build folder and not sync source");
                return;
            }

            // Get the repo endpoint and source provider.
            if (!TrySetPrimaryEndpointAndProviderInfo(executionContext))
            {
                throw new Exception(StringUtil.Loc("SupportedRepositoryEndpointNotFound"));
            }

            executionContext.Debug($"Primary repository: {SourceEndpoint.Name}. repository type: {SourceProvider.RepositoryType}");

            // Prepare the build directory.
            executionContext.Debug("Preparing build directory.");
            TrackingConfig trackingConfig = directoryManager.PrepareDirectory(
                executionContext,
                SourceEndpoint,
                SourceProvider);

            executionContext.Debug("Set build variables.");
            string _workDirectory = IOUtil.GetWorkPath(HostContext);
            executionContext.Variables.Set(Constants.Variables.Agent.BuildFolder, Path.Combine(_workDirectory, trackingConfig.BuildDirectory));
            executionContext.Variables.Set(Constants.Variables.System.ArtifactsDirectory, Path.Combine(_workDirectory, trackingConfig.ArtifactsDirectory));
            executionContext.Variables.Set(Constants.Variables.System.DefaultWorkingDirectory, Path.Combine(_workDirectory, trackingConfig.SourcesDirectory));
            executionContext.Variables.Set(Constants.Variables.Common.TestResultsDirectory, Path.Combine(_workDirectory, trackingConfig.TestResultsDirectory));

            executionContext.Variables.Set(Constants.Variables.Build.BinariesFolder, Path.Combine(_workDirectory, trackingConfig.BuildDirectory, Constants.Build.Path.BinariesDirectory));
            executionContext.Variables.Set(Constants.Variables.Build.SourceFolder, Path.Combine(_workDirectory, trackingConfig.SourcesDirectory));
            executionContext.Variables.Set(Constants.Variables.Build.StagingFolder, Path.Combine(_workDirectory, trackingConfig.ArtifactsDirectory));
            executionContext.Variables.Set(Constants.Variables.Build.ArtifactStagingFolder, Path.Combine(_workDirectory, trackingConfig.ArtifactsDirectory));

            executionContext.Variables.Set(Constants.Variables.Build.RepoId, SourceEndpoint.Id.ToString("D"));
            executionContext.Variables.Set(Constants.Variables.Build.RepoName, SourceEndpoint.Name);
            executionContext.Variables.Set(Constants.Variables.Build.RepoProvider, SourceEndpoint.Type);
            executionContext.Variables.Set(Constants.Variables.Build.RepoUri, SourceEndpoint.Url?.AbsoluteUri);
            executionContext.Variables.Set(Constants.Variables.Build.RepoLocalPath, Path.Combine(_workDirectory, trackingConfig.SourcesDirectory));

            string checkoutSubmoduleText;
            if (SourceEndpoint.Data.TryGetValue(WellKnownEndpointData.CheckoutSubmodules, out checkoutSubmoduleText))
            {
                executionContext.Variables.Set(Constants.Variables.Build.RepoGitSubmoduleCheckout, checkoutSubmoduleText);
            }

            // overwrite primary repository's clean value if build.repository.clean is sent from server. this is used by tfvc gated check-in
            bool? repoClean = executionContext.Variables.GetBoolean(Constants.Variables.Build.RepoClean);
            if (repoClean != null)
            {
                SourceEndpoint.Data[WellKnownEndpointData.Clean] = repoClean.Value.ToString();
            }
            else
            {
                string cleanRepoText;
                if(SourceEndpoint.Data.TryGetValue(WellKnownEndpointData.Clean, out cleanRepoText))
                {
                    // TODO: expandVariable
                    executionContext.Variables.Set(Constants.Variables.Build.RepoClean, cleanRepoText);
                }
            }

            executionContext.Debug($"Sync source for endpoint: {SourceEndpoint.Name}");
            await SourceProvider.GetSourceAsync(executionContext, SourceEndpoint, executionContext.CancellationToken);
        }

        private async Task FinallyAsync()
        {
            // Validate args.
            Trace.Entering();
            ArgUtil.NotNull(FinallyStep, nameof(FinallyStep));
            ArgUtil.NotNull(FinallyStep.ExecutionContext, nameof(FinallyStep.ExecutionContext));
            IExecutionContext executionContext = FinallyStep.ExecutionContext;

            // If syncSources = false, we will not reset repository.
            bool syncSources = executionContext.Variables.Build_SyncSources ?? true;
            if (!syncSources)
            {
                Trace.Verbose($"{Constants.Variables.Build.SyncSources} = false, we will not run post job cleanup for this repository");
                return;
            }

            await SourceProvider.PostJobCleanupAsync(executionContext, SourceEndpoint);
        }

        private bool TrySetPrimaryEndpointAndProviderInfo(IExecutionContext executionContext)
        {
            // Return the first service endpoint that contains a supported source provider.
            Trace.Entering();
            var extensionManager = HostContext.GetService<IExtensionManager>();
            List<ISourceProvider> sourceProviders = extensionManager.GetExtensions<ISourceProvider>();
            foreach (ServiceEndpoint ep in executionContext.Endpoints)
            {
                SourceProvider = sourceProviders
                    .FirstOrDefault(x => string.Equals(x.RepositoryType, ep.Type, StringComparison.OrdinalIgnoreCase));
                if (SourceProvider != null)
                {
                    SourceEndpoint = ep;
                    return true;
                }
            }

            return false;
        }
    }
}