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
using GitHub.DistributedTask.Pipelines.Expressions;
using System.Text;

namespace GitHub.Runner.Plugins.Repository.v1_1
{
    public class CheckoutTask : IRunnerActionPlugin
    {
        public async Task RunAsync(RunnerActionPluginExecutionContext executionContext, CancellationToken token)
        {
            string runnerWorkspace = executionContext.GetRunnerContext("workspace");
            ArgUtil.Directory(runnerWorkspace, nameof(runnerWorkspace));
            string tempDirectory = executionContext.GetRunnerContext("temp");
            ArgUtil.Directory(tempDirectory, nameof(tempDirectory));

            var repoFullName = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Repository);
            if (string.IsNullOrEmpty(repoFullName))
            {
                repoFullName = executionContext.GetGitHubContext("repository");
            }

            var repoFullNameSplit = repoFullName.Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (repoFullNameSplit.Length != 2)
            {
                throw new ArgumentOutOfRangeException(repoFullName);
            }

            string expectRepoPath;
            var path = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Path);
            if (!string.IsNullOrEmpty(path))
            {
                expectRepoPath = IOUtil.ResolvePath(runnerWorkspace, path);
                if (!expectRepoPath.StartsWith(runnerWorkspace.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar))
                {
                    throw new ArgumentException($"Input path '{path}' should resolve to a directory under '{runnerWorkspace}', current resolved path '{expectRepoPath}'.");
                }
            }
            else
            {
                // When repository doesn't has path set, default to sources directory 1/repoName
                expectRepoPath = Path.Combine(runnerWorkspace, repoFullNameSplit[1]);
            }

            var workspaceRepo = executionContext.GetGitHubContext("repository");
            // for self repository, we need to let the worker knows where it is after checkout.
            if (string.Equals(workspaceRepo, repoFullName, StringComparison.OrdinalIgnoreCase))
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

            string sourceBranch;
            string sourceVersion;
            string refInput = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Ref);
            if (string.IsNullOrEmpty(refInput))
            {
                sourceBranch = executionContext.GetGitHubContext("ref");
                sourceVersion = executionContext.GetGitHubContext("sha");
            }
            else
            {
                sourceBranch = refInput;
                sourceVersion = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Version);  // version get removed when checkout move to repo in the graph
                if (string.IsNullOrEmpty(sourceVersion) && RegexUtility.IsMatch(sourceBranch, WellKnownRegularExpressions.SHA1))
                {
                    sourceVersion = sourceBranch;
                    // If Ref is a SHA and the repo is self, we need to use github.ref as source branch since it might be refs/pull/*
                    if (string.Equals(workspaceRepo, repoFullName, StringComparison.OrdinalIgnoreCase))
                    {
                        sourceBranch = executionContext.GetGitHubContext("ref");
                    }
                    else
                    {
                        sourceBranch = "refs/heads/master";
                    }
                }
            }

            bool clean = StringUtil.ConvertToBoolean(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Clean), true);
            string submoduleInput = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules);

            int fetchDepth = 0;
            if (!int.TryParse(executionContext.GetInput("fetch-depth"), out fetchDepth) || fetchDepth < 0)
            {
                fetchDepth = 0;
            }

            bool gitLfsSupport = StringUtil.ConvertToBoolean(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs));
            string accessToken = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Token);
            if (string.IsNullOrEmpty(accessToken))
            {
                accessToken = executionContext.GetGitHubContext("token");
            }

            // register problem matcher
            string matcherFile = Path.Combine(tempDirectory, $"git_{Guid.NewGuid()}.json");
            File.WriteAllText(matcherFile, GitHubSourceProvider.ProblemMatcher, new UTF8Encoding(false));
            executionContext.Output($"##[add-matcher]{matcherFile}");
            try
            {
                await new GitHubSourceProvider().GetSourceAsync(executionContext,
                                                                expectRepoPath,
                                                                repoFullName,
                                                                sourceBranch,
                                                                sourceVersion,
                                                                clean,
                                                                submoduleInput,
                                                                fetchDepth,
                                                                gitLfsSupport,
                                                                accessToken,
                                                                token);
            }
            finally
            {
                executionContext.Output("##[remove-matcher owner=checkout-git]");
            }
        }
    }

    public class CleanupTask : IRunnerActionPlugin
    {
        public async Task RunAsync(RunnerActionPluginExecutionContext executionContext, CancellationToken token)
        {
            string tempDirectory = executionContext.GetRunnerContext("temp");
            ArgUtil.Directory(tempDirectory, nameof(tempDirectory));

            // register problem matcher
            string matcherFile = Path.Combine(tempDirectory, $"git_{Guid.NewGuid()}.json");
            File.WriteAllText(matcherFile, GitHubSourceProvider.ProblemMatcher, new UTF8Encoding(false));
            executionContext.Output($"##[add-matcher]{matcherFile}");
            try
            {
                await new GitHubSourceProvider().CleanupAsync(executionContext);
            }
            finally
            {
                executionContext.Output("##[remove-matcher owner=checkout-git]");
            }
        }
    }
}
