using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Agent.Sdk;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Agent.Plugins.Repository
{
    public interface ISourceProvider
    {
        Task GetSourceAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository, CancellationToken cancellationToken);

        Task PostJobCleanupAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository);
    }

    public abstract class RepositoryTask : IAgentTaskPlugin
    {
        public Guid Id => Pipelines.PipelineConstants.CheckoutTask.Id;
        public string Version => Pipelines.PipelineConstants.CheckoutTask.Version;

        public abstract string Stage { get; }

        public abstract Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token);

        protected ISourceProvider GetSourceProvider(string repositoryType)
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

    public class CheckoutTask : RepositoryTask
    {
        public override string Stage => "main";

        public override async Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token)
        {
            var sourceSkipVar = StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("agent.source.skip")?.Value);
            if (sourceSkipVar)
            {
                executionContext.Output($"Skip sync source for repository.");
                return;
            }

            var repoAlias = executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Repository, true);
            var repo = executionContext.Repositories.Single(x => string.Equals(x.Alias, repoAlias, StringComparison.OrdinalIgnoreCase));

            ISourceProvider sourceProvider = GetSourceProvider(repo.Type);
            await sourceProvider.GetSourceAsync(executionContext, repo, token);
        }
    }

    public class CleanupTask : RepositoryTask
    {
        public override string Stage => "post";

        public override async Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token)
        {
            var repoAlias = executionContext.TaskVariables.GetValueOrDefault("repository")?.Value;
            var repo = executionContext.Repositories.Single(x => string.Equals(x.Alias, repoAlias, StringComparison.OrdinalIgnoreCase));
            ArgUtil.NotNull(repo, nameof(repo));

            ISourceProvider sourceProvider = GetSourceProvider(repo.Type);
            await sourceProvider.PostJobCleanupAsync(executionContext, repo);
        }
    }
}
