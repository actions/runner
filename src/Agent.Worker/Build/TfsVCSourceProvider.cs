using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Agent;
using System;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.Agent.Worker.Build
{
    public sealed class TfsVCSourceProvider : SourceProvider, ISourceProvider
    {
        public override string RepositoryType => WellKnownRepositoryTypes.TfsVersionControl;

        public Task GetSourceAsync(IExecutionContext executionContext, ServiceEndpoint endpoint, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task PostJobCleanupAsync(IExecutionContext executionContext, ServiceEndpoint endpoint)
        {
            throw new NotImplementedException();
        }

        public string GetLocalPath(ServiceEndpoint endpoint, string path)
        {
            throw new NotImplementedException();
        }
    }
}