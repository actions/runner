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
