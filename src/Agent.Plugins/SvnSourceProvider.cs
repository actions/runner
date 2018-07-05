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
using Microsoft.VisualStudio.Services.Agent.Util;

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
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(repository, nameof(repository));

            SvnCliManager svn = new SvnCliManager();
            svn.Init(executionContext, repository, cancellationToken);

            // Determine the sources directory.
            string sourceDirectory = repository.Properties.Get<string>("path");
            executionContext.Debug($"sourceDirectory={sourceDirectory}");
            ArgUtil.NotNullOrEmpty(sourceDirectory, nameof(sourceDirectory));

            string sourceBranch = repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Ref);
            executionContext.Debug($"sourceBranch={sourceBranch}");

            string revision = repository.Version;
            if (string.IsNullOrWhiteSpace(revision))
            {
                revision = "HEAD";
            }

            executionContext.Debug($"revision={revision}");

            bool clean = StringUtil.ConvertToBoolean(executionContext.GetInput(Pipelines.PipelineConstants.CheckoutTaskInputs.Clean));
            executionContext.Debug($"clean={clean}");

            // Get the definition mappings.
            var mappings = repository.Properties.Get<IList<Pipelines.WorkspaceMapping>>(Pipelines.RepositoryPropertyNames.Mappings);
            List<SvnMappingDetails> allMappings = mappings.Select(x => new SvnMappingDetails() { ServerPath = x.ServerPath, LocalPath = x.LocalPath, Revision = x.Revision, Depth = x.Depth, IgnoreExternals = x.IgnoreExternals }).ToList();

            if (StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("system.debug")?.Value))
            {
                allMappings.ForEach(m => executionContext.Debug($"ServerPath: {m.ServerPath}, LocalPath: {m.LocalPath}, Depth: {m.Depth}, Revision: {m.Revision}, IgnoreExternals: {m.IgnoreExternals}"));
            }

            Dictionary<string, SvnMappingDetails> normalizedMappings = svn.NormalizeMappings(allMappings);
            if (StringUtil.ConvertToBoolean(executionContext.Variables.GetValueOrDefault("system.debug")?.Value))
            {
                executionContext.Debug($"Normalized mappings count: {normalizedMappings.Count}");
                normalizedMappings.ToList().ForEach(p => executionContext.Debug($"    [{p.Key}] ServerPath: {p.Value.ServerPath}, LocalPath: {p.Value.LocalPath}, Depth: {p.Value.Depth}, Revision: {p.Value.Revision}, IgnoreExternals: {p.Value.IgnoreExternals}"));
            }

            string normalizedBranch = svn.NormalizeRelativePath(sourceBranch, '/', '\\');

            executionContext.Output(StringUtil.Loc("SvnSyncingRepo", repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Name)));

            string effectiveRevision = await svn.UpdateWorkspace(
                sourceDirectory,
                normalizedMappings,
                clean,
                normalizedBranch,
                revision);

            executionContext.Output(StringUtil.Loc("SvnBranchCheckedOut", normalizedBranch, repository.Properties.Get<string>(Pipelines.RepositoryPropertyNames.Name), effectiveRevision));
        }

        public Task PostJobCleanupAsync(AgentTaskPluginExecutionContext executionContext, Pipelines.RepositoryResource repository)
        {
            return Task.CompletedTask;
        }
    }
}