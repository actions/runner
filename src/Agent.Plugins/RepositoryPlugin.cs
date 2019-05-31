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

    public abstract class RepositoryTask : IAgentTaskPlugin
    {
        private static readonly HashSet<String> _checkoutOptions = new HashSet<String>(StringComparer.OrdinalIgnoreCase)
        {
            Pipelines.PipelineConstants.CheckoutTaskInputs.Clean,
            Pipelines.PipelineConstants.CheckoutTaskInputs.FetchDepth,
            Pipelines.PipelineConstants.CheckoutTaskInputs.Lfs,
            Pipelines.PipelineConstants.CheckoutTaskInputs.PersistCredentials,
            Pipelines.PipelineConstants.CheckoutTaskInputs.Submodules,
        };

        protected RepositoryTask()
            : this(new SourceProviderFactory())
        {
        }

        protected RepositoryTask(ISourceProviderFactory sourceProviderFactory)
        {
            SourceProviderFactory = sourceProviderFactory;
        }

        public Guid Id => Pipelines.PipelineConstants.CheckoutTask.Id;
        public string Version => Pipelines.PipelineConstants.CheckoutTask.Version;

        public ISourceProviderFactory SourceProviderFactory { get; }

        public abstract string Stage { get; }

        public abstract Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token);

        protected void MergeCheckoutOptions(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository)
        {
            // Merge the repository checkout options
            if ((!executionContext.Variables.TryGetValue("MERGE_CHECKOUT_OPTIONS", out VariableValue mergeCheckoutOptions) || !String.Equals(mergeCheckoutOptions.Value, "false", StringComparison.OrdinalIgnoreCase)) &&
                repository.Properties.Get<JToken>(Pipelines.RepositoryPropertyNames.CheckoutOptions) is JObject checkoutOptions)
            {
                foreach (var pair in checkoutOptions)
                {
                    var inputName = pair.Key;

                    // Skip if unexpected checkout option
                    if (!_checkoutOptions.Contains(inputName))
                    {
                        executionContext.Debug($"Unexpected checkout option '{inputName}'");
                        continue;
                    }

                    // Skip if input defined
                    if (executionContext.Inputs.TryGetValue(inputName, out string inputValue) && !string.IsNullOrEmpty(inputValue))
                    {
                        continue;
                    }

                    try
                    {
                        executionContext.Inputs[inputName] = pair.Value.ToObject<String>();
                    }
                    catch (Exception ex)
                    {
                        executionContext.Debug($"Error setting the checkout option '{inputName}': {ex.Message}");
                    }
                }
            }
        }
    }

    public class CheckoutTask : RepositoryTask
    {
        private readonly Regex _validSha1 = new Regex(@"\b[0-9a-f]{40}\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        public CheckoutTask()
        {
        }

        public CheckoutTask(ISourceProviderFactory sourceProviderFactory)
            : base(sourceProviderFactory)
        {
        }

        public override string Stage => "main";

        public override async Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token)
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

    public class CleanupTask : RepositoryTask
    {
        public override string Stage => "post";

        public override async Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token)
        {
            var repoAlias = executionContext.TaskVariables.GetValueOrDefault("repository")?.Value;
            if (!string.IsNullOrEmpty(repoAlias))
            {
                var repo = executionContext.Repositories.Single(x => string.Equals(x.Alias, repoAlias, StringComparison.OrdinalIgnoreCase));
                ArgUtil.NotNull(repo, nameof(repo));

                MergeCheckoutOptions(executionContext, repo);

                ISourceProvider sourceProvider = SourceProviderFactory.GetSourceProvider(repo.Type);
                await sourceProvider.PostJobCleanupAsync(executionContext, repo);
            }
        }
    }

    public interface ISourceProviderFactory
    {
        ISourceProvider GetSourceProvider(string repositoryType);
    }

    public sealed class SourceProviderFactory : ISourceProviderFactory
    {
        public ISourceProvider GetSourceProvider(string repositoryType)
        {
            ISourceProvider sourceProvider = null;

            if (string.Equals(repositoryType, Pipelines.RepositoryTypes.GitHub, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(repositoryType, Pipelines.RepositoryTypes.GitHubEnterprise, StringComparison.OrdinalIgnoreCase))
            {
                sourceProvider = new GitHubSourceProvider();
            }
            else if (string.Equals(repositoryType, Pipelines.RepositoryTypes.Bitbucket, StringComparison.OrdinalIgnoreCase))
            {
                sourceProvider = new BitbucketGitSourceProvider();
            }
            else if (string.Equals(repositoryType, Pipelines.RepositoryTypes.ExternalGit, StringComparison.OrdinalIgnoreCase))
            {
                sourceProvider = new ExternalGitSourceProvider();
            }
            else if (string.Equals(repositoryType, Pipelines.RepositoryTypes.Git, StringComparison.OrdinalIgnoreCase))
            {
                sourceProvider = new TfsGitSourceProvider();
            }
            else if (string.Equals(repositoryType, Pipelines.RepositoryTypes.Tfvc, StringComparison.OrdinalIgnoreCase))
            {
                sourceProvider = new TfsVCSourceProvider();
            }
            else if (string.Equals(repositoryType, Pipelines.RepositoryTypes.Svn, StringComparison.OrdinalIgnoreCase))
            {
                sourceProvider = new SvnSourceProvider();
            }
            else
            {
                throw new NotSupportedException(repositoryType);
            }

            return sourceProvider;
        }
    }
}
