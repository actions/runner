using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class LocalRunSourceProvider : SourceProvider, ISourceProvider
    {
        public override string RepositoryType => "LocalRun";

        public Task GetSourceAsync(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            CancellationToken cancellationToken)
        {
            Trace.Entering();
            ArgUtil.Equal(RunMode.Local, HostContext.RunMode, nameof(HostContext.RunMode));
            ArgUtil.Equal(HostTypes.Build, executionContext.Variables.System_HostType, nameof(executionContext.Variables.System_HostType));
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(endpoint, nameof(endpoint));

            bool preferGitFromPath;
#if OS_WINDOWS
            preferGitFromPath = executionContext.Variables.GetBoolean(Constants.Variables.System.PreferGitFromPath) ?? false;
#else
            preferGitFromPath = true;
#endif
            if (!preferGitFromPath)
            {
                // Add git to the PATH.
                string gitPath = Path.Combine(HostContext.GetDirectory(WellKnownDirectory.Externals), "git", "cmd", $"git{IOUtil.ExeExtension}");
                ArgUtil.File(gitPath, nameof(gitPath));
                executionContext.Output(StringUtil.Loc("Prepending0WithDirectoryContaining1", Constants.PathVariable, Path.GetFileName(gitPath)));
                var varUtil = HostContext.GetService<IVarUtil>();
                varUtil.PrependPath(Path.GetDirectoryName(gitPath));
                executionContext.Debug($"{Constants.PathVariable}: '{Environment.GetEnvironmentVariable(Constants.PathVariable)}'");
            }
            else
            {
                // Validate git is in the PATH.
                var whichUtil = HostContext.GetService<IWhichUtil>();
                whichUtil.Which("git", require: true);
            }

            // Override build.sourcesDirectory.
            //
            // Technically the value will be out of sync with the tracking file. The tracking file
            // is created during job initialization (Get Sources is later). That is OK, since the
            // local-run-sources-directory should not participate in cleanup anyway.
            string localDirectory = endpoint.Data?["localDirectory"];
            ArgUtil.Directory(localDirectory, nameof(localDirectory));
            ArgUtil.Directory(Path.Combine(localDirectory, ".git"), "localDotGitDirectory");
            executionContext.Variables.Set(Constants.Variables.System.DefaultWorkingDirectory, localDirectory);
            executionContext.Variables.Set(Constants.Variables.Build.SourcesDirectory, localDirectory);
            executionContext.Variables.Set(Constants.Variables.Build.RepoLocalPath, localDirectory);

            // todo: consider support for clean

            return Task.CompletedTask;
        }

        public Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            return Task.CompletedTask;
        }
    }
}