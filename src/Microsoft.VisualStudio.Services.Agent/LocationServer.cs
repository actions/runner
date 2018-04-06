using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Location.Client;
using Microsoft.VisualStudio.Services.Location;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(LocationServer))]
    public interface ILocationServer : IAgentService
    {
        Task ConnectAsync(VssConnection jobConnection);

        Task<ConnectionData> GetConnectionDataAsync();
    }

    public sealed class LocationServer : AgentService, ILocationServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private LocationHttpClient _locationClient;

        public async Task ConnectAsync(VssConnection jobConnection)
        {
            _connection = jobConnection;
            int attemptCount = 5;
            while (!_connection.HasAuthenticated && attemptCount-- > 0)
            {
                try
                {
                    await _connection.ConnectAsync();
                    break;
                }
                catch (Exception ex) when (attemptCount > 0)
                {
                    Trace.Info($"Catch exception during connect. {attemptCount} attempt left.");
                    Trace.Error(ex);
                }

                await Task.Delay(100);
            }

            _locationClient = _connection.GetClient<LocationHttpClient>();
            _hasConnection = true;
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException("SetConnection");
            }
        }

        public async Task<ConnectionData> GetConnectionDataAsync()
        {
            CheckConnection();
            return await _locationClient.GetConnectionDataAsync(ConnectOptions.None, 0);
        }
    }
}