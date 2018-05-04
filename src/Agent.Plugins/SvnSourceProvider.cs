using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Newtonsoft.Json;
using Agent.Sdk;
using Pipelines = Microsoft.TeamFoundation.DistributedTask.Pipelines;

namespace Agent.Plugins.Repository
{
    public sealed class SvnSourceProvider : ISourceProvider
    {
        public async Task GetSourceAsync(
            AgentTaskPluginExecutionContext executionContext,
            Pipelines.RepositoryResource repository,
            CancellationToken cancellationToken)
        {
            // Validate args.
            PluginUtil.NotNull(executionContext, nameof(executionContext));
            PluginUtil.NotNull(repository, nameof(repository));

            SvnCliManager svn = new SvnCliManager();
            svn.Init(executionContext, repository, cancellationToken);

            // Determine the sources directory.
            string sourcesDirectory = repository.Properties.Get<string>("sourcedirecotry");
            executionContext.Debug($"sourcesDirectory={sourcesDirectory}");
            PluginUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));

            string sourceBranch = repository.Properties.Get<string>("sourcebranch");
            executionContext.Debug($"sourceBranch={sourceBranch}");

            string revision = repository.Version;
            if (string.IsNullOrWhiteSpace(revision))
            {
                revision = "HEAD";
            }

            executionContext.Debug($"revision={revision}");

            bool clean = PluginUtil.ConvertToBoolean(repository.Properties.Get<string>(EndpointData.Clean));
            executionContext.Debug($"clean={clean}");

            // Get the definition mappings.
            List<SvnMappingDetails> allMappings = JsonConvert.DeserializeObject<SvnWorkspace>(repository.Properties.Get<string>(EndpointData.SvnWorkspaceMapping)).Mappings;

            if (PluginUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("system.debug")?.Value))
            {
                allMappings.ForEach(m => executionContext.Debug($"ServerPath: {m.ServerPath}, LocalPath: {m.LocalPath}, Depth: {m.Depth}, Revision: {m.Revision}, IgnoreExternals: {m.IgnoreExternals}"));
            }

            Dictionary<string, SvnMappingDetails> normalizedMappings = svn.NormalizeMappings(allMappings);
            if (PluginUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("system.debug")?.Value))
            {
                executionContext.Debug($"Normalized mappings count: {normalizedMappings.Count}");
                normalizedMappings.ToList().ForEach(p => executionContext.Debug($"    [{p.Key}] ServerPath: {p.Value.ServerPath}, LocalPath: {p.Value.LocalPath}, Depth: {p.Value.Depth}, Revision: {p.Value.Revision}, IgnoreExternals: {p.Value.IgnoreExternals}"));
            }

            string normalizedBranch = svn.NormalizeRelativePath(sourceBranch, '/', '\\');

            executionContext.Output(PluginUtil.Loc("SvnSyncingRepo", repository.Properties.Get<string>("name")));

            string effectiveRevision = await svn.UpdateWorkspace(
                sourcesDirectory,
                normalizedMappings,
                clean,
                normalizedBranch,
                revision);

            executionContext.Output(PluginUtil.Loc("SvnBranchCheckedOut", normalizedBranch, repository.Properties.Get<string>("name"), effectiveRevision));
        }

        public Task PostJobCleanupAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository)
        {
            return Task.CompletedTask;
        }
    }
}