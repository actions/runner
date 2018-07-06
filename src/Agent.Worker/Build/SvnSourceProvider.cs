using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.DistributedTask.Pipelines;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class SvnSourceProvider : SourceProvider, ISourceProvider
    {
        public override string RepositoryType => TeamFoundation.DistributedTask.Pipelines.RepositoryTypes.Svn;

        public async Task GetSourceAsync(
            IExecutionContext executionContext,
            ServiceEndpoint endpoint,
            CancellationToken cancellationToken)
        {
            Trace.Entering();

            // Validate args.
            ArgUtil.NotNull(executionContext, nameof(executionContext));
            ArgUtil.NotNull(endpoint, nameof(endpoint));

            ISvnCommandManager svn = HostContext.CreateService<ISvnCommandManager>();
            svn.Init(executionContext, endpoint, cancellationToken);

            // Determine the sources directory.
            string sourcesDirectory = GetEndpointData(endpoint, Constants.EndpointData.SourcesDirectory);
            executionContext.Debug($"sourcesDirectory={sourcesDirectory}");
            ArgUtil.NotNullOrEmpty(sourcesDirectory, nameof(sourcesDirectory));

            string sourceBranch = GetEndpointData(endpoint, Constants.EndpointData.SourceBranch);
            executionContext.Debug($"sourceBranch={sourceBranch}");

            string revision = GetEndpointData(endpoint, Constants.EndpointData.SourceVersion);
            if (string.IsNullOrWhiteSpace(revision))
            {
                revision = "HEAD";
            }

            executionContext.Debug($"revision={revision}");

            bool clean = endpoint.Data.ContainsKey(EndpointData.Clean) &&
                StringUtil.ConvertToBoolean(endpoint.Data[EndpointData.Clean], defaultValue: false);
            executionContext.Debug($"clean={clean}");

            // Get the definition mappings.
            executionContext.Debug(endpoint.Data[EndpointData.SvnWorkspaceMapping]);
            List<SvnMappingDetails> allMappings = JsonConvert.DeserializeObject<SvnWorkspace>
                (endpoint.Data[EndpointData.SvnWorkspaceMapping]).Mappings;

            if (executionContext.Variables.System_Debug.HasValue && executionContext.Variables.System_Debug.Value)
            {
                allMappings.ForEach(m => executionContext.Debug($"ServerPath: {m.ServerPath}, LocalPath: {m.LocalPath}, Depth: {m.Depth}, Revision: {m.Revision}, IgnoreExternals: {m.IgnoreExternals}"));
            }

            Dictionary<string, SvnMappingDetails> normalizedMappings = svn.NormalizeMappings(allMappings);
            if (executionContext.Variables.System_Debug.HasValue && executionContext.Variables.System_Debug.Value)
            {
                executionContext.Debug($"Normalized mappings count: {normalizedMappings.Count}");
                normalizedMappings.ToList().ForEach(p => executionContext.Debug($"    [{p.Key}] ServerPath: {p.Value.ServerPath}, LocalPath: {p.Value.LocalPath}, Depth: {p.Value.Depth}, Revision: {p.Value.Revision}, IgnoreExternals: {p.Value.IgnoreExternals}"));
            }

            string normalizedBranch = svn.NormalizeRelativePath(sourceBranch, '/', '\\');

            executionContext.Output(StringUtil.Loc("SvnSyncingRepo", endpoint.Name));

            string effectiveRevision = await svn.UpdateWorkspace(
                sourcesDirectory,
                normalizedMappings,
                clean,
                normalizedBranch,
                revision);

            executionContext.Output(StringUtil.Loc("SvnBranchCheckedOut", normalizedBranch, endpoint.Name, effectiveRevision));
            Trace.Verbose("Leaving SvnSourceProvider.GetSourceAsync");
        }

        public override string GetLocalPath(IExecutionContext executionContext, RepositoryResource repository, string path)
        {
            Trace.Verbose("Entering SvnSourceProvider.GetLocalPath");

            ISvnCommandManager svn = HostContext.CreateService<ISvnCommandManager>();
            svn.Init(executionContext, repository, CancellationToken.None);

            // We assume that this is a server path first.
            string serverPath = svn.NormalizeRelativePath(path, '/', '\\').Trim();
            string localPath;

            if (serverPath.StartsWith("^/"))
            {
                // Convert the server path to the relative one using SVN work copy mappings
                string sourcesDirectory = repository.Properties.Get<string>(RepositoryPropertyNames.Path);
                localPath = svn.ResolveServerPath(serverPath, sourcesDirectory);
            }
            else
            {
                // normalize the path back to the local file system one.
                localPath = svn.NormalizeRelativePath(serverPath, Path.DirectorySeparatorChar, '/');
            }

            Trace.Verbose("Leaving SvnSourceProvider.GetLocalPath");
            return localPath;
        }

        public override void SetVariablesInEndpoint(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            base.SetVariablesInEndpoint(executionContext, endpoint);
            endpoint.Data.Add(Constants.EndpointData.SourceBranch, executionContext.Variables.Get(Constants.Variables.Build.SourceBranch));
        }

        public Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            return Task.CompletedTask;
        }
    }
}