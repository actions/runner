using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Agent.Sdk;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json.Linq;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using System.IO;
using Microsoft.TeamFoundation.DistributedTask.Pipelines.ContextData;
using Microsoft.VisualStudio.Services.WebApi;
using System.Text.RegularExpressions;

namespace Agent.Plugins.Repository
{
    public interface ISourceProvider
    {
        Task GetSourceAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository, CancellationToken cancellationToken);

        Task PostJobCleanupAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository);
    }

    public class CheckoutTask : IAgentTaskPlugin
    {
        private readonly Regex _validSha1 = new Regex(@"\b[0-9a-f]{40}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        public string Stage => "main";
        public Guid Id => Pipelines.PipelineConstants.CheckoutTask.Id;
        public string Version => Pipelines.PipelineConstants.CheckoutTask.Version;

        public async Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token)
        {
            var runnerContext = executionContext.Context["runner"] as DictionaryContextData;
            string pipelineWorkspace = runnerContext.GetValueOrDefault("pipelineWorkspace") as StringContextData;
            ArgUtil.Directory(pipelineWorkspace, nameof(pipelineWorkspace));
            var repoAlias = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Repository, true);

            Pipelines.RepositoryResource repo = null;
            if (string.Equals(repoAlias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                repo = executionContext.Repositories.Single(x => string.Equals(x.Alias, repoAlias, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var repoSplit = repoAlias.Split("@", StringSplitOptions.RemoveEmptyEntries);
                if (repoSplit.Length != 2)
                {
                    throw new ArgumentOutOfRangeException(repoAlias);
                }

                var repoNameSplit = repoSplit[0].Split("/", StringSplitOptions.RemoveEmptyEntries);
                if (repoNameSplit.Length != 2)
                {
                    throw new ArgumentOutOfRangeException(repoSplit[0]);
                }

                repo = new Pipelines.RepositoryResource()
                {
                    Id = repoSplit[0],
                    Type = Pipelines.RepositoryTypes.GitHub,
                    Alias = Guid.NewGuid().ToString(),
                    Url = new Uri($"https://github.com/{repoSplit[0]}")
                };

                if (_validSha1.IsMatch(repoSplit[1]))
                {
                    repo.Version = repoSplit[1];
                }
                else
                {
                    repo.Properties.Set(Pipelines.RepositoryPropertyNames.Ref, repoSplit[1]);
                }

                repo.Properties.Set(Pipelines.RepositoryPropertyNames.Name, repoSplit[0]);
                repo.Properties.Set(Pipelines.RepositoryPropertyNames.Path, Path.Combine(pipelineWorkspace, repoNameSplit[1]));
            }

            var currentRepoPath = repo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
            string tempDirectory = runnerContext.GetValueOrDefault("tempdirectory") as StringContextData;

            ArgUtil.NotNullOrEmpty(currentRepoPath, nameof(currentRepoPath));
            ArgUtil.NotNullOrEmpty(tempDirectory, nameof(tempDirectory));

            string expectRepoPath;
            var path = executionContext.GetInput("path");
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
                var repoName = repo.Properties.Get<String>(Pipelines.RepositoryPropertyNames.Name);
                var repoNameSplit = repoName.Split("/", StringSplitOptions.RemoveEmptyEntries);
                if (repoNameSplit.Length != 2)
                {
                    throw new ArgumentOutOfRangeException(repoName);
                }

                expectRepoPath = Path.Combine(pipelineWorkspace, repoNameSplit[1]);
            }

            // for self repository, we need to let the agent knows where it is after checkout.
            if (string.Equals(repoAlias, Pipelines.PipelineConstants.SelfAlias, StringComparison.OrdinalIgnoreCase))
            {
                executionContext.UpdateSelfRepositoryPath(expectRepoPath);
            }

            executionContext.Debug($"Repository requires to be placed at '{expectRepoPath}', current location is '{currentRepoPath}'");
            if (!string.Equals(currentRepoPath.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), expectRepoPath.Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), IOUtil.FilePathStringComparison))
            {
                executionContext.Output($"Repository is current at '{currentRepoPath}', move to '{expectRepoPath}'.");
                var count = 1;
                var staging = Path.Combine(tempDirectory, $"_{count}");
                while (Directory.Exists(staging))
                {
                    count++;
                    staging = Path.Combine(tempDirectory, $"_{count}");
                }

                try
                {
                    executionContext.Debug($"Move existing repository '{currentRepoPath}' to '{expectRepoPath}' via staging directory '{staging}'.");
                    IOUtil.MoveDirectory(currentRepoPath, expectRepoPath, staging, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    executionContext.Debug("Catch exception during repository move.");
                    executionContext.Debug(ex.ToString());
                    executionContext.Warning("Unable move and reuse existing repository to required location.");
                    IOUtil.DeleteDirectory(expectRepoPath, CancellationToken.None);
                }

                executionContext.Output($"Repository will locate at '{expectRepoPath}'.");
                repo.Properties.Set<string>(Pipelines.RepositoryPropertyNames.Path, expectRepoPath);
            }

            await new GitHubSourceProvider().GetSourceAsync(executionContext, repo, token);
        }
    }
}
