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
            var sourceSkipVar = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("agent.source.skip")?.Value) ||
                                !StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("build.syncSources")?.Value ?? bool.TrueString);
            if (sourceSkipVar)
            {
                executionContext.Output($"Skip sync source for repository.");
                return;
            }

            var repoAlias = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Repository, true);
            var repo = executionContext.Repositories.Single(x => string.Equals(x.Alias, repoAlias, StringComparison.OrdinalIgnoreCase));

            MergeCheckoutOptions(executionContext, repo);

            var currentRepoPath = repo.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Path);
            var buildDirectory = executionContext.Variables.GetValueOrDefault("agent.builddirectory")?.Value;
            var tempDirectory = executionContext.Variables.GetValueOrDefault("agent.tempdirectory")?.Value;

            ArgUtil.NotNullOrEmpty(currentRepoPath, nameof(currentRepoPath));
            ArgUtil.NotNullOrEmpty(buildDirectory, nameof(buildDirectory));
            ArgUtil.NotNullOrEmpty(tempDirectory, nameof(tempDirectory));

            string expectRepoPath;
            var path = executionContext.GetInput("path");
            if (!string.IsNullOrEmpty(path))
            {
                expectRepoPath = IOUtil.ResolvePath(buildDirectory, path);
                if (!expectRepoPath.StartsWith(buildDirectory.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar))
                {
                    throw new ArgumentException($"Input path '{path}' should resolve to a directory under '{buildDirectory}', current resolved path '{expectRepoPath}'.");
                }
            }
            else
            {
                // When repository doesn't has path set, default to sources directory 1/s
                expectRepoPath = Path.Combine(buildDirectory, "s");
            }

            executionContext.UpdateRepositoryPath(repoAlias, expectRepoPath);

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

            ISourceProvider sourceProvider = SourceProviderFactory.GetSourceProvider(repo.Type);
            await sourceProvider.GetSourceAsync(executionContext, repo, token);
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

            if (string.Equals(repositoryType, Pipelines.RepositoryTypes.Bitbucket, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(repositoryType, Pipelines.RepositoryTypes.GitHub, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(repositoryType, Pipelines.RepositoryTypes.GitHubEnterprise, StringComparison.OrdinalIgnoreCase))
            {
                sourceProvider = new AuthenticatedGitSourceProvider();
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
