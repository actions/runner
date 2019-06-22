using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Runner.Sdk;
using Pipelines = GitHub.DistributedTask.Pipelines;
using System.IO;
using GitHub.DistributedTask.Pipelines.ContextData;
using System.Text.RegularExpressions;

namespace GitHub.Runner.Plugins.Repository
{
    public interface ISourceProvider
    {
        Task GetSourceAsync(RunnerActionPluginExecutionContext executionContext, string repositoryPath, CancellationToken cancellationToken);
    }

    public class CheckoutTask : IRunnerActionPlugin
    {
        private readonly Regex _validSha1 = new Regex(@"\b[0-9a-f]{40}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        public async Task RunAsync(RunnerActionPluginExecutionContext executionContext, CancellationToken token)
        {
            string pipelineWorkspace = executionContext.GetRunnerContext("pipelineWorkspace");
            ArgUtil.Directory(pipelineWorkspace, nameof(pipelineWorkspace));
            string tempDirectory = executionContext.GetRunnerContext("tempdirectory");
            ArgUtil.Directory(tempDirectory, nameof(tempDirectory));

            var repoFullName = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Repository, true);
            var repoFullNameSplit = repoFullName.Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (repoFullNameSplit.Length != 2)
            {
                throw new ArgumentOutOfRangeException(repoFullName);
            }

            string expectRepoPath;
            var path = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Path);
            if (!string.IsNullOrEmpty(path))
            {
                expectRepoPath = IOUtil.ResolvePath(pipelineWorkspace, path);
                if (!expectRepoPath.StartsWith(pipelineWorkspace.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar))
                {
                    throw new ArgumentException($"Input path '{path}' should resolve to a directory under '{pipelineWorkspace}', current resolved path '{expectRepoPath}'.");
                }
            }
            else
            {
                // When repository doesn't has path set, default to sources directory 1/repoName
                expectRepoPath = Path.Combine(pipelineWorkspace, repoFullNameSplit[1]);
            }

            var workspaceRepo = StringUtil.ConvertToBoolean(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.WorkspaceRepo));
            // for self repository, we need to let the worker knows where it is after checkout.
            if (workspaceRepo)
            {
                var workspaceRepoPath = executionContext.GetGitHubContext("workspace");

                executionContext.Debug($"Repository requires to be placed at '{expectRepoPath}', current location is '{workspaceRepoPath}'");
                if (!string.Equals(workspaceRepoPath.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), expectRepoPath.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), IOUtil.FilePathStringComparison))
                {
                    executionContext.Output($"Repository is current at '{workspaceRepoPath}', move to '{expectRepoPath}'.");
                    var count = 1;
                    var staging = Path.Combine(tempDirectory, $"_{count}");
                    while (Directory.Exists(staging))
                    {
                        count++;
                        staging = Path.Combine(tempDirectory, $"_{count}");
                    }

                    try
                    {
                        executionContext.Debug($"Move existing repository '{workspaceRepoPath}' to '{expectRepoPath}' via staging directory '{staging}'.");
                        IOUtil.MoveDirectory(workspaceRepoPath, expectRepoPath, staging, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        executionContext.Debug("Catch exception during repository move.");
                        executionContext.Debug(ex.ToString());
                        executionContext.Warning("Unable move and reuse existing repository to required location.");
                        IOUtil.DeleteDirectory(expectRepoPath, CancellationToken.None);
                    }

                    executionContext.Output($"Repository will locate at '{expectRepoPath}'.");
                }

                executionContext.Debug($"Update workspace repository location.");
                executionContext.SetRepositoryPath(repoFullName, expectRepoPath, true);
            }

            await new GitHubSourceProvider().GetSourceAsync(executionContext, expectRepoPath, token);
        }
    }
}
