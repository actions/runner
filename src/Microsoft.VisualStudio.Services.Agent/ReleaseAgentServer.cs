using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(ReleaseAgentServer))]
    public interface IReleaseAgentServer : IAgentService
    {
        IEnumerable<AgentArtifactDefinition> GetReleaseArtifactsFromService(
            CancellationToken cancellationToken,
            Guid teamProject,
            int releaseId);
    }

    public sealed class ReleaseAgentServer : AgentService, IReleaseAgentServer
    {
        private bool _hasConnection;

        private VssConnection _connection;

        private ReleaseHttpClient _releaseClient;

        public async Task ConnectAsync(VssConnection agentConnection)
        {
            this._connection = agentConnection;

            if (!this._connection.HasAuthenticated)
            {
                await this._connection.ConnectAsync();
            }

            this._releaseClient = this._connection.GetClient<ReleaseHttpClient>();
            this._hasConnection = true;
        }

        private void CheckConnection()
        {
            if (!this._hasConnection)
            {
                throw new InvalidOperationException("SetConnection");
            }
        }

        public IEnumerable<AgentArtifactDefinition> GetReleaseArtifactsFromService(CancellationToken cancellationToken, Guid teamProject, int releaseId)
        {
            CheckConnection();
            var artifacts = _releaseClient.GetAgentArtifactDefinitionsAsync(teamProject, releaseId, cancellationToken: cancellationToken).Result;
            return artifacts;
        }
    }
}