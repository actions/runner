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

namespace Agent.Plugins.Repository
{
    public interface ISourceProvider
    {
        Task GetSourceAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository, CancellationToken cancellationToken);

        Task PostJobCleanupAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository);
    }

    public class CheckoutTask : IAgentTaskPlugin
    {
        public string FriendlyName => "Get Sources";
        public Guid Id => new Guid("c61807ba-5e20-4b70-bd8c-3683c9f74003");
        public string Version => "1.0.0";
        public string Description => "Get Sources";
        public string HelpMarkDown => "";
        public string Author => "Microsoft";

        public TaskInputDefinition[] Inputs => new TaskInputDefinition[] {
            new TaskInputDefinition()
            {
                Name="repository",
                InputType = TaskInputType.String,
                DefaultValue="self",
                Required=true
            }
        };

        public HashSet<string> Stages => new HashSet<string>() { "main", "post" };

        public async Task RunAsync(AgentTaskPluginExecutionContext executionContext, CancellationToken token)
        {
            var repoAlias = executionContext.GetInput("repository", true);
            var repo = executionContext.Repositories.Single(x => string.Equals(x.Alias, repoAlias, StringComparison.OrdinalIgnoreCase));
            MergeInputs(executionContext, repo);

            ISourceProvider sourceProvider = null;
            switch (repo.Type)
            {
                case RepositoryTypes.Bitbucket:
                case RepositoryTypes.GitHub:
                case RepositoryTypes.GitHubEnterprise:
                    sourceProvider = new AuthenticatedGitSourceProvider();
                    break;
                case RepositoryTypes.Git:
                    sourceProvider = new ExternalGitSourceProvider();
                    break;
                case RepositoryTypes.TfsGit:
                    sourceProvider = new TfsGitSourceProvider();
                    break;
                case RepositoryTypes.TfsVersionControl:
                    sourceProvider = new TfsVCSourceProvider();
                    break;
                case RepositoryTypes.Svn:
                    sourceProvider = new SvnSourceProvider();
                    break;
                default:
                    throw new NotSupportedException(repo.Type);
            }

            if (executionContext.Stage == "main")
            {
                await sourceProvider.GetSourceAsync(executionContext, repo, token);
            }
            else if (executionContext.Stage == "post")
            {
                await sourceProvider.PostJobCleanupAsync(executionContext, repo);
            }
        }

        private void MergeInputs(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository)
        {
            string clean = executionContext.GetInput("clean");
            if (!string.IsNullOrEmpty(clean))
            {
                repository.Properties.Set<bool>("clean", PluginUtil.ConvertToBoolean(clean));
            }

            // there is no addition inputs for TFVC and SVN
            if (repository.Type == RepositoryTypes.Bitbucket ||
                repository.Type == RepositoryTypes.GitHub ||
                repository.Type == RepositoryTypes.GitHubEnterprise ||
                repository.Type == RepositoryTypes.Git ||
                repository.Type == RepositoryTypes.TfsGit)
            {
                string checkoutSubmodules = executionContext.GetInput("checkoutSubmodules");
                if (!string.IsNullOrEmpty(checkoutSubmodules))
                {
                    repository.Properties.Set<bool>("checkoutSubmodules", PluginUtil.ConvertToBoolean(checkoutSubmodules));
                }

                string checkoutNestedSubmodules = executionContext.GetInput("checkoutNestedSubmodules");
                if (!string.IsNullOrEmpty(checkoutNestedSubmodules))
                {
                    repository.Properties.Set<bool>("checkoutNestedSubmodules", PluginUtil.ConvertToBoolean(checkoutNestedSubmodules));
                }

                string preserveCredential = executionContext.GetInput("preserveCredential");
                if (!string.IsNullOrEmpty(preserveCredential))
                {
                    repository.Properties.Set<bool>("preserveCredential", PluginUtil.ConvertToBoolean(preserveCredential));
                }

                string gitLfsSupport = executionContext.GetInput("gitLfsSupport");
                if (!string.IsNullOrEmpty(gitLfsSupport))
                {
                    repository.Properties.Set<bool>("gitLfsSupport", PluginUtil.ConvertToBoolean(gitLfsSupport));
                }

                string acceptUntrustedCerts = executionContext.GetInput("acceptUntrustedCerts");
                if (!string.IsNullOrEmpty(acceptUntrustedCerts))
                {
                    repository.Properties.Set<bool>("acceptUntrustedCerts", PluginUtil.ConvertToBoolean(acceptUntrustedCerts));
                }

                string fetchDepth = executionContext.GetInput("fetchDepth");
                if (!string.IsNullOrEmpty(fetchDepth))
                {
                    repository.Properties.Set<string>("fetchDepth", fetchDepth);
                }
            }
        }
    }
}
